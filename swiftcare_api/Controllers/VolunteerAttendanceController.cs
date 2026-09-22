using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using swiftcare_api.Data;
using swiftcare_api.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace swiftcare_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VolunteerAttendanceController : ControllerBase
    {
        private readonly swiftcare_apiDbContext _context;
        private readonly ILogger<VolunteerAttendanceController> _logger;

        public VolunteerAttendanceController(
            swiftcare_apiDbContext context,
            ILogger<VolunteerAttendanceController> logger)
        {
            _context = context;
            _logger = logger;
        }


        // ============================================================
        // CHECK IN
        // ============================================================

        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn(
            [FromBody] VolunteerCheckInRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Check-in request is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.VolunteerId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "VolunteerId is required."
                });
            }

            if (request.VolunteerRosterShiftId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "VolunteerRosterShiftId is required."
                });
            }

            // ========================================================
            // FIND VOLUNTEER
            // ========================================================

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(
                    v => v.Id == request.VolunteerId,
                    cancellationToken);

            if (volunteer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Volunteer could not be found."
                });
            }

            // ========================================================
            // FIND ROSTER SHIFT
            // ========================================================

            var shift = await _context.VolunteerRosterShifts
                .Include(s => s.VolunteerRoster)
                .FirstOrDefaultAsync(
                    s =>
                        s.VolunteerRosterShiftId ==
                        request.VolunteerRosterShiftId &&
                        s.VolunteerRoster!.VolunteerId ==
                        request.VolunteerId,
                    cancellationToken);

            if (shift == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "The roster shift could not be found for this volunteer."
                });
            }

            // ========================================================
            // CHECK SHIFT DATE
            // ========================================================

            var today = DateTime.Today;

            if (shift.ShiftDate.Date != today)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This roster shift is not scheduled for today.",
                    shiftDate = shift.ShiftDate.Date,
                    currentDate = today
                });
            }

            // ========================================================
            // CHECK SHIFT STATUS
            // ========================================================

            if (shift.Status == VolunteerRosterShiftStatus.Cancelled)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This shift has been cancelled."
                });
            }

            if (shift.Status == VolunteerRosterShiftStatus.DayOff)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This volunteer has a day off today."
                });
            }

            // ========================================================
            // CHECK SHIFT TIMES
            // ========================================================

            if (shift.StartTime == TimeSpan.Zero ||
                shift.EndTime == TimeSpan.Zero)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This shift does not have valid start and end times."
                });
            }

            // ========================================================
            // PREVENT DUPLICATE CHECK-IN
            // ========================================================

            var attendance = await _context.VolunteerAttendances
                .FirstOrDefaultAsync(
                    a =>
                        a.VolunteerId == request.VolunteerId &&
                        a.VolunteerRosterShiftId ==
                        request.VolunteerRosterShiftId &&
                        a.AttendanceDate.Date == today,
                    cancellationToken);

            if (attendance != null &&
                attendance.CheckInTime.HasValue)
            {
                return Conflict(new
                {
                    success = false,
                    message = "The volunteer has already checked in for this shift.",
                    attendanceId = attendance.AttendanceId,
                    checkInTime = attendance.CheckInTime
                });
            }

            // ========================================================
            // CURRENT TIME
            // ========================================================

            var checkInTime = DateTime.Now;

            var scheduledStart = today
                .Add(shift.StartTime);

            // ========================================================
            // CALCULATE LATE MINUTES
            // ========================================================

            var lateMinutes = 0;

            if (checkInTime > scheduledStart)
            {
                lateMinutes =
                    (int)Math.Floor(
                        (checkInTime - scheduledStart).TotalMinutes);
            }

            // ========================================================
            // CREATE ATTENDANCE
            // ========================================================

            if (attendance == null)
            {
                attendance = new VolunteerAttendance
                {
                    VolunteerId = volunteer.Id,

                    VolunteerRosterShiftId =
                        shift.VolunteerRosterShiftId,

                    AttendanceDate = today,

                    CheckInTime = checkInTime,

                    CheckOutTime = null,

                    Status =
                        lateMinutes > 0
                            ? VolunteerAttendanceStatus.Late
                            : VolunteerAttendanceStatus.Present,

                    LateMinutes = lateMinutes,

                    EarlyDepartureMinutes = 0,

                    FaceVerified = false,

                    LivenessVerified = false,

                    VerificationProvider = null,

                    VerificationReference = null,

                    CreatedDateTime = checkInTime,

                    ModifiedDateTime = null,

                    CreatedBy = volunteer.Id,

                    ModifiedBy = null
                };

                _context.VolunteerAttendances.Add(attendance);
            }
            else
            {
                attendance.CheckInTime = checkInTime;

                attendance.Status =
                    lateMinutes > 0
                        ? VolunteerAttendanceStatus.Late
                        : VolunteerAttendanceStatus.Present;

                attendance.LateMinutes = lateMinutes;

                attendance.ModifiedDateTime = checkInTime;
                attendance.ModifiedBy = volunteer.Id;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Volunteer {VolunteerId} checked in for roster shift {ShiftId}. Attendance {AttendanceId}.",
                volunteer.Id,
                shift.VolunteerRosterShiftId,
                attendance.AttendanceId);

            // ========================================================
            // RESPONSE
            // ========================================================

            return Ok(new
            {
                success = true,
                message =
                    lateMinutes > 0
                        ? "Volunteer checked in late."
                        : "Volunteer checked in successfully.",

                attendanceId = attendance.AttendanceId,

                volunteerId = volunteer.Id,

                volunteerName =
                    $"{volunteer.FirstName} {volunteer.LastName}".Trim(),

                volunteerRosterShiftId =
                    shift.VolunteerRosterShiftId,

                attendanceDate =
                    attendance.AttendanceDate,

                checkInTime =
                    attendance.CheckInTime,

                scheduledStart,

                lateMinutes =
                    attendance.LateMinutes,

                status =
                    attendance.Status.ToString()
            });
        }


        // ============================================================
        // CHECK OUT
        // ============================================================

        [HttpPost("check-out")]
        public async Task<IActionResult> CheckOut(
            [FromBody] VolunteerCheckOutRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Check-out request is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.VolunteerId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "VolunteerId is required."
                });
            }

            if (request.VolunteerRosterShiftId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "VolunteerRosterShiftId is required."
                });
            }

            // ========================================================
            // FIND VOLUNTEER
            // ========================================================

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(
                    v => v.Id == request.VolunteerId,
                    cancellationToken);

            if (volunteer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Volunteer could not be found."
                });
            }

            // ========================================================
            // FIND SHIFT
            // ========================================================

            var shift = await _context.VolunteerRosterShifts
                .Include(s => s.VolunteerRoster)
                .FirstOrDefaultAsync(
                    s =>
                        s.VolunteerRosterShiftId ==
                        request.VolunteerRosterShiftId &&
                        s.VolunteerRoster!.VolunteerId ==
                        request.VolunteerId,
                    cancellationToken);

            if (shift == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "The roster shift could not be found for this volunteer."
                });
            }

            // ========================================================
            // FIND TODAY'S ATTENDANCE
            // ========================================================

            var today = DateTime.Today;

            var attendance = await _context.VolunteerAttendances
                .FirstOrDefaultAsync(
                    a =>
                        a.VolunteerId == request.VolunteerId &&
                        a.VolunteerRosterShiftId ==
                        request.VolunteerRosterShiftId &&
                        a.AttendanceDate.Date == today,
                    cancellationToken);

            if (attendance == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The volunteer has not checked in for this shift."
                });
            }

            // ========================================================
            // CHECK-IN REQUIRED
            // ========================================================

            if (!attendance.CheckInTime.HasValue)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The volunteer has not checked in for this shift."
                });
            }

            // ========================================================
            // PREVENT DUPLICATE CHECK-OUT
            // ========================================================

            if (attendance.CheckOutTime.HasValue)
            {
                return Conflict(new
                {
                    success = false,
                    message = "The volunteer has already checked out for this shift.",

                    attendanceId =
                        attendance.AttendanceId,

                    checkOutTime =
                        attendance.CheckOutTime
                });
            }

            // ========================================================
            // CHECK SHIFT STATUS
            // ========================================================

            if (shift.Status == VolunteerRosterShiftStatus.Cancelled)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This shift has been cancelled."
                });
            }

            if (shift.Status == VolunteerRosterShiftStatus.DayOff)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This shift is marked as a day off."
                });
            }

            // ========================================================
            // CHECK SHIFT TIMES
            // ========================================================

            if (shift.StartTime == TimeSpan.Zero ||
                shift.EndTime == TimeSpan.Zero)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This shift does not have valid start and end times."
                });
            }

            // ========================================================
            // CURRENT TIME
            // ========================================================

            var checkOutTime = DateTime.Now;

            var scheduledEnd = today
                .Add(shift.EndTime);

            // ========================================================
            // CALCULATE EARLY DEPARTURE
            // ========================================================

            var earlyDepartureMinutes = 0;

            if (checkOutTime < scheduledEnd)
            {
                earlyDepartureMinutes =
                    (int)Math.Floor(
                        (scheduledEnd - checkOutTime).TotalMinutes);
            }

            // ========================================================
            // UPDATE ATTENDANCE
            // ========================================================

            attendance.CheckOutTime = checkOutTime;

            attendance.EarlyDepartureMinutes =
                earlyDepartureMinutes;

            attendance.Status =
                VolunteerAttendanceStatus.CheckedOut;

            attendance.ModifiedDateTime = checkOutTime;

            attendance.ModifiedBy = volunteer.Id;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Volunteer {VolunteerId} checked out from roster shift {ShiftId}. Attendance {AttendanceId}.",
                volunteer.Id,
                shift.VolunteerRosterShiftId,
                attendance.AttendanceId);

            // ========================================================
            // RESPONSE
            // ========================================================

            return Ok(new
            {
                success = true,

                message =
                    earlyDepartureMinutes > 0
                        ? "Volunteer checked out early."
                        : "Volunteer checked out successfully.",

                attendanceId =
                    attendance.AttendanceId,

                volunteerId =
                    volunteer.Id,

                volunteerName =
                    $"{volunteer.FirstName} {volunteer.LastName}".Trim(),

                volunteerRosterShiftId =
                    shift.VolunteerRosterShiftId,

                attendanceDate =
                    attendance.AttendanceDate,

                checkInTime =
                    attendance.CheckInTime,

                checkOutTime =
                    attendance.CheckOutTime,

                scheduledEnd,

                lateMinutes =
                    attendance.LateMinutes,

                earlyDepartureMinutes =
                    attendance.EarlyDepartureMinutes,

                status =
                    attendance.Status.ToString()
            });
        }
    }


    // ================================================================
    // CHECK-IN REQUEST
    // ================================================================

    public class VolunteerCheckInRequest
    {
        [Required]
        public string VolunteerId { get; set; } = string.Empty;

        [Required]
        public int VolunteerRosterShiftId { get; set; }
    }


    // ================================================================
    // CHECK-OUT REQUEST
    // ================================================================

    public class VolunteerCheckOutRequest
    {
        [Required]
        public string VolunteerId { get; set; } = string.Empty;

        [Required]
        public int VolunteerRosterShiftId { get; set; }
    }
}