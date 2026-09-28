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
                double? latitude,
                double? longitude,
                double? accuracy,
                CancellationToken cancellationToken = default)
        {
            // ============================================================
            // VALIDATE REQUIRED VALUES
            // ============================================================

            if (string.IsNullOrWhiteSpace(volunteerId))
            {
                throw new ArgumentException(
                    "Volunteer ID is required.",
                    nameof(volunteerId));
            }

            if (string.IsNullOrWhiteSpace(diditSessionId))
            {
                throw new ArgumentException(
                    "Didit session ID is required.",
                    nameof(diditSessionId));
            }

            // ============================================================
            // VALIDATE GPS COORDINATES
            // ============================================================

            if (latitude.HasValue &&
                (latitude.Value < -90 || latitude.Value > 90))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(latitude),
                    "Latitude must be between -90 and 90.");
            }

            if (longitude.HasValue &&
                (longitude.Value < -180 || longitude.Value > 180))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(longitude),
                    "Longitude must be between -180 and 180.");
            }

            if (accuracy.HasValue && accuracy.Value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(accuracy),
                    "Location accuracy cannot be negative.");
            }

            // ============================================================
            // SOUTH AFRICA TIME ZONE
            // ============================================================

            // Windows uses:
            // "South Africa Standard Time"
            //
            // Linux uses:
            // "Africa/Johannesburg"

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

            // ============================================================
            // CURRENT DATE / TIME
            // ============================================================

            var nowUtc = DateTime.UtcNow;

            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(
                nowUtc,
                localZone);

            var today = nowLocal.Date;

            var currentTime = nowLocal.TimeOfDay;

            // ============================================================
            // FIND TODAY'S SHIFTS FOR THIS VOLUNTEER
            // ============================================================

            // Only Scheduled and Completed roster shifts are eligible.
            //
            // DayOff and Cancelled shifts are excluded.
            //
            // VolunteerRoster.VolunteerId is assumed to be the
            // volunteer's final Identity ID.

            var shifts = await _dbContext.VolunteerRosterShifts
                .AsNoTracking()
                .Where(s =>
                    s.VolunteerRoster != null &&
                    s.VolunteerRoster.VolunteerId == volunteerId &&
                    s.ShiftDate.Date == today &&
                    (
                        s.Status == VolunteerRosterShiftStatus.Scheduled ||
                        s.Status == VolunteerRosterShiftStatus.Completed
                    ))
                .OrderBy(s => s.StartTime)
                .ToListAsync(cancellationToken);

            // ============================================================
            // CHECK FOR EXISTING ATTENDANCE
            // ============================================================

            // An existing attendance takes precedence over the
            // current clock time.
            //
            // This matters if the volunteer reopens the app after
            // their scheduled shift has ended.

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

            // ============================================================
            // EXISTING ATTENDANCE - ALREADY CHECKED OUT
            // ============================================================

            if (existing != null)
            {
                if (existing.CheckOutTime.HasValue)
                {
                    return new FaceLoginAttendanceResult
                    {
                        CheckedIn = false,

                        AlreadyCheckedIn = false,

                        Status = "CheckedOut",

                        Message =
                            "You have already checked out of this shift.",

                        AttendanceId =
                            existing.AttendanceId,

                        VolunteerRosterShiftId =
                            existing.VolunteerRosterShiftId,

                        CheckInTime =
                            existing.CheckInTime,

                        LateMinutes =
                            existing.LateMinutes
                    };
                }

                // ========================================================
                // EXISTING ATTENDANCE - ALREADY CHECKED IN
                // ========================================================

                if (existing.CheckInTime.HasValue)
                {
                    return new FaceLoginAttendanceResult
                    {
                        CheckedIn = true,

                        AlreadyCheckedIn = true,

                        Status =
                            existing.Status.ToString(),

                        Message =
                            "You are already checked in. Your original " +
                            "check-in time has been preserved.",

                        AttendanceId =
                            existing.AttendanceId,

                        VolunteerRosterShiftId =
                            existing.VolunteerRosterShiftId,

                        CheckInTime =
                            existing.CheckInTime,

                        LateMinutes =
                            existing.LateMinutes
                    };
                }
            }

            // ============================================================
            // FIND CURRENT SHIFT
            // ============================================================

            // Check-in is permitted only from the scheduled start time
            // through the scheduled end time.
            //
            // This version assumes shifts do not cross midnight.

            var currentShift = shifts.FirstOrDefault(s =>
                currentTime >= s.StartTime &&
                currentTime < s.EndTime);

            // ============================================================
            // NO CURRENT SHIFT
            // ============================================================

            if (currentShift == null)
            {
                return new FaceLoginAttendanceResult
                {
                    CheckedIn = false,

                    AlreadyCheckedIn = false,

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

            // ============================================================
            // PROTECT AGAINST CONCURRENT CHECK-IN REQUESTS
            // ============================================================

            // Serializable isolation helps protect against two
            // simultaneous face-login status requests creating
            // duplicate attendance records.

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            // ============================================================
            // DOUBLE-CHECK ATTENDANCE INSIDE TRANSACTION
            // ============================================================

            var attendance =
                await _dbContext.VolunteerAttendances
                    .FirstOrDefaultAsync(
                        a =>
                            a.VolunteerRosterShiftId ==
                            currentShift.VolunteerRosterShiftId,
                        cancellationToken);

            // ============================================================
            // ATTENDANCE WAS CREATED BY ANOTHER REQUEST
            // ============================================================

            if (attendance != null)
            {
                await transaction.CommitAsync(
                    cancellationToken);

                return new FaceLoginAttendanceResult
                {
                    CheckedIn =
                        attendance.CheckInTime.HasValue &&
                        !attendance.CheckOutTime.HasValue,

                    AlreadyCheckedIn =
                        attendance.CheckInTime.HasValue &&
                        !attendance.CheckOutTime.HasValue,

                    Status =
                        attendance.CheckOutTime.HasValue
                            ? "CheckedOut"
                            : attendance.Status.ToString(),

                    Message =
                        attendance.CheckOutTime.HasValue
                            ? "You have already checked out of this shift."
                            : "Your existing attendance has been preserved.",

                    AttendanceId =
                        attendance.AttendanceId,

                    VolunteerRosterShiftId =
                        attendance.VolunteerRosterShiftId,

                    CheckInTime =
                        attendance.CheckInTime,

                    LateMinutes =
                        attendance.LateMinutes
                };
            }

            // ============================================================
            // CALCULATE LATE MINUTES
            // ============================================================

            // Exactly 15 minutes late is still Present.
            //
            // More than 15 minutes late is Late.

            var minutesAfterStart =
                (
                    nowLocal -
                    today.Add(currentShift.StartTime)
                ).TotalMinutes;

            var lateMinutes = Math.Max(
                0,
                (int)Math.Floor(minutesAfterStart));

            var isLate =
                minutesAfterStart > 15;

            // ============================================================
            // CREATE ATTENDANCE
            // ============================================================

            attendance = new Models.VolunteerAttendance
            {
                // ========================================================
                // VOLUNTEER / SHIFT
                // ========================================================

                VolunteerId =
                    volunteerId,

                VolunteerRosterShiftId =
                    currentShift.VolunteerRosterShiftId,

                // ========================================================
                // DATE / TIME
                // ========================================================

                AttendanceDate =
                    today,

                CheckInTime =
                    nowUtc,

                CheckOutTime =
                    null,

                // ========================================================
                // ATTENDANCE STATUS
                // ========================================================

                Status =
                    isLate
                        ? VolunteerAttendanceStatus.Late
                        : VolunteerAttendanceStatus.Present,

                LateMinutes =
                    lateMinutes,

                EarlyDepartureMinutes =
                    0,

                // ========================================================
                // DIDIT FACE VERIFICATION
                // ========================================================

                FaceVerified =
                    true,

                LivenessVerified =
                    true,

                VerificationProvider =
                    "Didit",

                VerificationReference =
                    diditSessionId,

                // ========================================================
                // GPS LOCATION
                // ========================================================

                CheckInLatitude =
                    latitude,

                CheckInLongitude =
                    longitude,

                CheckInLocationAccuracy =
                    accuracy,

                // ========================================================
                // AUDIT
                // ========================================================

                CreatedDateTime =
                    nowUtc,

                ModifiedDateTime =
                    null
            };

            // ============================================================
            // SAVE ATTENDANCE
            // ============================================================

            _dbContext.VolunteerAttendances.Add(
                attendance);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            // ============================================================
            // LOG CHECK-IN
            // ============================================================

            _logger.LogInformation(
                "Volunteer {VolunteerId} checked in for shift " +
                "{ShiftId}. AttendanceId: {AttendanceId}. " +
                "Status: {Status}. LateMinutes: {LateMinutes}. " +
                "Latitude: {Latitude}. Longitude: {Longitude}. " +
                "Accuracy: {Accuracy}m.",
                volunteerId,
                currentShift.VolunteerRosterShiftId,
                attendance.AttendanceId,
                attendance.Status,
                attendance.LateMinutes,
                latitude,
                longitude,
                accuracy);

            // ============================================================
            // RETURN RESULT
            // ============================================================

            return new FaceLoginAttendanceResult
            {
                CheckedIn = true,

                AlreadyCheckedIn = false,

                Status =
                    attendance.Status.ToString(),

                Message = isLate
                    ? "Face login successful. You have been checked in late."
                    : "Face login successful. You have been checked in.",

                AttendanceId =
                    attendance.AttendanceId,

                VolunteerRosterShiftId =
                    attendance.VolunteerRosterShiftId,

                CheckInTime =
                    attendance.CheckInTime,

                LateMinutes =
                    attendance.LateMinutes
            };
        }
    }
}