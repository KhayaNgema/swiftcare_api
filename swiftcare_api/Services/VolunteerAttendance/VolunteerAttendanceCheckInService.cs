
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using swiftcare_api.Data;
using swiftcare_api.Models;
using swiftcare_api.Services.VolunteerAttendance;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace swiftcare_api.Services.VolunteerAttendance
{
    public sealed class VolunteerAttendanceCheckInService
        : IVolunteerAttendanceCheckInService
    {
        private readonly swiftcare_apiDbContext _dbContext;
        private readonly ILogger<VolunteerAttendanceCheckInService> _logger;

        public VolunteerAttendanceCheckInService(
            swiftcare_apiDbContext dbContext,
            ILogger<VolunteerAttendanceCheckInService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<FaceLoginAttendanceResult>
            CheckInAfterFaceLoginAsync(
                string volunteerId,
                string diditSessionId,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(volunteerId))
                throw new ArgumentException(
                    "Volunteer ID is required.",
                    nameof(volunteerId));

            if (string.IsNullOrWhiteSpace(diditSessionId))
                throw new ArgumentException(
                    "Didit session ID is required.",
                    nameof(diditSessionId));

            // Windows uses "South Africa Standard Time".
            // Linux uses "Africa/Johannesburg".
            TimeZoneInfo localZone;

            try
            {
                localZone = TimeZoneInfo.FindSystemTimeZoneById(
                    "Africa/Johannesburg");
            }
            catch (TimeZoneNotFoundException)
            {
                localZone = TimeZoneInfo.FindSystemTimeZoneById(
                    "South Africa Standard Time");
            }

            var nowUtc = DateTime.UtcNow;
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(
                nowUtc,
                localZone);

            var today = nowLocal.Date;
            var currentTime = nowLocal.TimeOfDay;

            // Read the shift for THIS volunteer, on THIS date.
            // Only Scheduled and Completed roster shifts are
            // considered eligible; DayOff and Cancelled are excluded.
            //
            // Assumes VolunteerRoster.VolunteerId is the volunteer's
            // final Identity ID.

            var shifts = await _dbContext.VolunteerRosterShifts
                .AsNoTracking()
                .Where(s =>
                    s.VolunteerRoster != null &&
                    s.VolunteerRoster.VolunteerId == volunteerId &&
                    s.ShiftDate.Date == today &&
                    (s.Status == VolunteerRosterShiftStatus.Scheduled ||
                     s.Status == VolunteerRosterShiftStatus.Completed))
                .OrderBy(s => s.StartTime)
                .ToListAsync(cancellationToken);

            // An existing attendance takes precedence over the
            // current clock time. This matters if the volunteer
            // reopens the app after their scheduled shift has ended.
            var shiftIds = shifts
                .Select(s => s.VolunteerRosterShiftId)
                .ToList();

            var existing = await _dbContext.VolunteerAttendances
                .AsNoTracking()
                .Where(a =>
                    a.VolunteerId == volunteerId &&
                    shiftIds.Contains(a.VolunteerRosterShiftId))
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing != null)
            {
                if (existing.CheckOutTime.HasValue)
                {
                    return new FaceLoginAttendanceResult
                    {
                        CheckedIn = false,
                        Status = "CheckedOut",
                        Message =
                            "You have already checked out of this shift.",
                        AttendanceId = existing.AttendanceId,
                        VolunteerRosterShiftId =
                            existing.VolunteerRosterShiftId,
                        CheckInTime = existing.CheckInTime,
                        LateMinutes = existing.LateMinutes
                    };
                }

                if (existing.CheckInTime.HasValue)
                {
                    return new FaceLoginAttendanceResult
                    {
                        CheckedIn = true,
                        AlreadyCheckedIn = true,
                        Status = existing.Status.ToString(),
                        Message =
                            "You are already checked in. Your original " +
                            "check-in time has been preserved.",
                        AttendanceId = existing.AttendanceId,
                        VolunteerRosterShiftId =
                            existing.VolunteerRosterShiftId,
                        CheckInTime = existing.CheckInTime,
                        LateMinutes = existing.LateMinutes
                    };
                }
            }

            // A check-in is permitted only from the scheduled
            // start time through the scheduled end time.
            // This version assumes shifts do not cross midnight.

            var currentShift = shifts.FirstOrDefault(s =>
                currentTime >= s.StartTime &&
                currentTime < s.EndTime);

            if (currentShift == null)
            {
                return new FaceLoginAttendanceResult
                {
                    CheckedIn = false,
                    Status = shifts.Count == 0
                        ? "NoShift"
                        : "OutsideShiftHours",
                    Message = shifts.Count == 0
                        ? "Face login successful. You have no eligible " +
                          "shift scheduled for today."
                        : "Face login successful. You are currently " +
                          "outside your scheduled shift hours."
                };
            }

            // Protect against concurrent face-login status requests.
            // Also configure the unique index shown below.
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var attendance = await _dbContext.VolunteerAttendances
                .FirstOrDefaultAsync(
                    a => a.VolunteerRosterShiftId ==
                         currentShift.VolunteerRosterShiftId,
                    cancellationToken);

            if (attendance != null)
            {
                await transaction.CommitAsync(cancellationToken);

                return new FaceLoginAttendanceResult
                {
                    CheckedIn =
                        attendance.CheckInTime.HasValue &&
                        !attendance.CheckOutTime.HasValue,
                    AlreadyCheckedIn =
                        attendance.CheckInTime.HasValue &&
                        !attendance.CheckOutTime.HasValue,
                    Status = attendance.CheckOutTime.HasValue
                        ? "CheckedOut"
                        : attendance.Status.ToString(),
                    Message = attendance.CheckOutTime.HasValue
                        ? "You have already checked out of this shift."
                        : "Your existing attendance has been preserved.",
                    AttendanceId = attendance.AttendanceId,
                    VolunteerRosterShiftId =
                        attendance.VolunteerRosterShiftId,
                    CheckInTime = attendance.CheckInTime,
                    LateMinutes = attendance.LateMinutes
                };
            }

            // Exactly 15 minutes late is still Present.
            // More than 15 minutes late is Late.
            var minutesAfterStart =
                (nowLocal - today.Add(currentShift.StartTime))
                .TotalMinutes;

            var lateMinutes = Math.Max(
                0,
                (int)Math.Floor(minutesAfterStart));

            var isLate = minutesAfterStart > 15;

            attendance = new Models.VolunteerAttendance
            {
                VolunteerId = volunteerId,
                VolunteerRosterShiftId =
                    currentShift.VolunteerRosterShiftId,
                AttendanceDate = today,
                CheckInTime = nowUtc,
                CheckOutTime = null,
                Status = isLate
                    ? VolunteerAttendanceStatus.Late
                    : VolunteerAttendanceStatus.Present,
                LateMinutes = lateMinutes,
                EarlyDepartureMinutes = 0,
                FaceVerified = true,
                LivenessVerified = true,
                VerificationProvider = "Didit",
                VerificationReference = diditSessionId,
                CreatedDateTime = nowUtc,
                ModifiedDateTime = null
            };

            _dbContext.VolunteerAttendances.Add(attendance);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Volunteer {VolunteerId} checked in for shift " +
                "{ShiftId}. AttendanceId: {AttendanceId}. " +
                "Status: {Status}. LateMinutes: {LateMinutes}",
                volunteerId,
                currentShift.VolunteerRosterShiftId,
                attendance.AttendanceId,
                attendance.Status,
                attendance.LateMinutes);

            return new FaceLoginAttendanceResult
            {
                CheckedIn = true,
                AlreadyCheckedIn = false,
                Status = attendance.Status.ToString(),
                Message = isLate
                    ? "Face login successful. You have been checked in late."
                    : "Face login successful. You have been checked in.",
                AttendanceId = attendance.AttendanceId,
                VolunteerRosterShiftId =
                    attendance.VolunteerRosterShiftId,
                CheckInTime = attendance.CheckInTime,
                LateMinutes = attendance.LateMinutes
            };
        }
    }
}