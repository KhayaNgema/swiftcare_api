using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using swiftcare_api.Data;
using swiftcare_api.DTOs;
using swiftcare_api.Models;

namespace swiftcare_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VolunteerScheduleController : ControllerBase
    {
        private readonly swiftcare_apiDbContext _context;
        private readonly ILogger<VolunteerScheduleController> _logger;

        public VolunteerScheduleController(
            swiftcare_apiDbContext context,
            ILogger<VolunteerScheduleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // GET LOGGED-IN VOLUNTEER ID
        //
        // The new JWT uses:
        //
        // ClaimTypes.NameIdentifier = Volunteer.Id
        //
        // We therefore use NameIdentifier first.
        // VolunteerId is kept as a fallback.
        // ============================================================

        private string? GetVolunteerId()
        {
            // --------------------------------------------------------
            // PRIMARY:
            // JWT NameIdentifier
            // --------------------------------------------------------

            var volunteerId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(volunteerId))
            {
                return volunteerId;
            }

            // --------------------------------------------------------
            // FALLBACK:
            // Custom VolunteerId claim
            // --------------------------------------------------------

            volunteerId =
                User.FindFirst("VolunteerId")?.Value;

            if (!string.IsNullOrWhiteSpace(volunteerId))
            {
                return volunteerId;
            }

            // --------------------------------------------------------
            // FALLBACK:
            // Lowercase volunteerId
            // --------------------------------------------------------

            volunteerId =
                User.FindFirst("volunteerId")?.Value;

            return volunteerId;
        }

        // ============================================================
        // VERIFY VOLUNTEER
        // ============================================================

        private async Task<bool> VolunteerExistsAsync(
            string volunteerId,
            CancellationToken cancellationToken)
        {
            return await _context.Volunteers
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == volunteerId,
                    cancellationToken);
        }

        // ============================================================
        // GET TODAY
        //
        // GET:
        // /api/VolunteerSchedule/today
        //
        // Flutter:
        // TodayTasksPage
        //
        // Returns:
        // - Today's date
        // - Today's shift
        // - Today's assigned tasks
        // ============================================================

        [HttpGet("today")]
        public async Task<IActionResult> GetToday(
            CancellationToken cancellationToken = default)
        {
            var volunteerId = GetVolunteerId();

            if (string.IsNullOrWhiteSpace(volunteerId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer identity could not be determined."
                });
            }

            var volunteerExists =
                await VolunteerExistsAsync(
                    volunteerId,
                    cancellationToken);

            if (!volunteerExists)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer account could not be found."
                });
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // ========================================================
            // TODAY'S TASKS
            // ========================================================

            var taskEntities =
                await _context.VolunteerTaskAssignments
                    .AsNoTracking()
                    .Include(x => x.VolunteerTask)
                    .Where(x =>
                        x.VolunteerId == volunteerId &&
                        x.TaskDate >= today &&
                        x.TaskDate < tomorrow &&
                        x.Status !=
                            VolunteerTaskAssignmentStatus.Cancelled)
                    .OrderBy(x => x.StartTime)
                    .ToListAsync(cancellationToken);

            var tasks =
                taskEntities
                    .Select(x => new VolunteerTaskDto
                    {
                        TaskId =
                             x.VolunteerTaskAssignmentId,

                        Title =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Title
                                : string.Empty,

                        Description =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Description
                                : null,

                        Area =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Area.ToString()
                                : string.Empty,

                        Location =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Location
                                : null,

                        Date =
                            x.TaskDate.ToString(
                                "yyyy-MM-dd"),

                        StartTime =
                            x.StartTime.ToString(
                                @"hh\:mm"),

                        EndTime =
                            x.EndTime.ToString(
                                @"hh\:mm"),

                        Status =
                            x.Status.ToString(),

                        Priority =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Priority.ToString()
                                : string.Empty
                    })
                    .ToList();

            // ========================================================
            // TODAY'S SHIFT
            // ========================================================

            var shift =
                await _context.VolunteerRosterShifts
                    .AsNoTracking()
                    .Include(x => x.VolunteerRoster)
                    .Where(x =>
                        x.VolunteerRoster != null &&
                        x.VolunteerRoster.VolunteerId ==
                            volunteerId &&
                        x.ShiftDate >= today &&
                        x.ShiftDate < tomorrow &&
                        x.Status !=
                            VolunteerRosterShiftStatus.Cancelled)
                    .OrderBy(x => x.StartTime)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            VolunteerShiftDto? shiftDto = null;

            if (shift != null)
            {
                shiftDto =
                    new VolunteerShiftDto
                    {
                        ShiftId =
                            shift.VolunteerRosterShiftId,

                        Date =
                            shift.ShiftDate.ToString(
                                "yyyy-MM-dd"),

                        StartTime =
                            shift.StartTime.ToString(
                                @"hh\:mm"),

                        EndTime =
                            shift.EndTime.ToString(
                                @"hh\:mm"),

                        Status =
                            shift.Status.ToString(),

                        Notes =
                            shift.Notes
                    };
            }

            // ========================================================
            // RESULT
            //
            // IMPORTANT:
            // Keep the schedule properties at the ROOT level so
            // Flutter can read:
            //
            // decoded['date']
            // decoded['shift']
            // decoded['tasks']
            //
            // ========================================================

            return Ok(new
            {
                success = true,

                date =
                    today.ToString("yyyy-MM-dd"),

                shift = shiftDto,

                tasks = tasks,

                totalTasks = tasks.Count
            });
        }

        // ============================================================
        // GET THIS WEEK
        //
        // GET:
        // /api/VolunteerSchedule/week
        //
        // Monday -> Sunday
        //
        // Flutter:
        // WeeklyTasksPage
        // ============================================================

        [HttpGet("week")]
        public async Task<IActionResult> GetThisWeek(
            CancellationToken cancellationToken = default)
        {
            var volunteerId = GetVolunteerId();

            if (string.IsNullOrWhiteSpace(volunteerId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer identity could not be determined."
                });
            }

            var volunteerExists =
                await VolunteerExistsAsync(
                    volunteerId,
                    cancellationToken);

            if (!volunteerExists)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer account could not be found."
                });
            }

            var today = DateTime.Today;

            // ========================================================
            // MONDAY
            // ========================================================

            var daysFromMonday =
                ((int)today.DayOfWeek + 6) % 7;

            var startDate =
                today.AddDays(-daysFromMonday);

            // Sunday
            var endDate =
                startDate.AddDays(6);

            // Exclusive upper boundary
            var endDateExclusive =
                startDate.AddDays(7);

            // ========================================================
            // TASKS
            // ========================================================

            var taskEntities =
                await _context.VolunteerTaskAssignments
                    .AsNoTracking()
                    .Include(x => x.VolunteerTask)
                    .Where(x =>
                        x.VolunteerId == volunteerId &&
                        x.TaskDate >= startDate &&
                        x.TaskDate < endDateExclusive &&
                        x.Status !=
                            VolunteerTaskAssignmentStatus.Cancelled)
                    .OrderBy(x => x.TaskDate)
                    .ThenBy(x => x.StartTime)
                    .ToListAsync(cancellationToken);

            var tasks =
                taskEntities
                    .Select(x => new VolunteerTaskDto
                    {
                        TaskId =
                            x.VolunteerTaskAssignmentId,

                        Title =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Title
                                : string.Empty,

                        Description =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Description
                                : null,

                        Area =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Area.ToString()
                                : string.Empty,

                        Location =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Location
                                : null,

                        Date =
                            x.TaskDate.ToString(
                                "yyyy-MM-dd"),

                        StartTime =
                            x.StartTime.ToString(
                                @"hh\:mm"),

                        EndTime =
                            x.EndTime.ToString(
                                @"hh\:mm"),

                        Status =
                            x.Status.ToString(),

                        Priority =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Priority.ToString()
                                : string.Empty
                    })
                    .ToList();

            // ========================================================
            // SHIFTS
            // ========================================================

            var shifts =
                await _context.VolunteerRosterShifts
                    .AsNoTracking()
                    .Include(x => x.VolunteerRoster)
                    .Where(x =>
                        x.VolunteerRoster != null &&
                        x.VolunteerRoster.VolunteerId ==
                            volunteerId &&
                        x.ShiftDate >= startDate &&
                        x.ShiftDate < endDateExclusive &&
                        x.Status !=
                            VolunteerRosterShiftStatus.Cancelled)
                    .OrderBy(x => x.ShiftDate)
                    .ThenBy(x => x.StartTime)
                    .ToListAsync(cancellationToken);

            // ========================================================
            // BUILD MONDAY -> SUNDAY
            // ========================================================

            var days =
                new List<VolunteerDayScheduleDto>();

            for (var i = 0; i < 7; i++)
            {
                var date =
                    startDate.AddDays(i);

                var dateString =
                    date.ToString("yyyy-MM-dd");

                var dayTasks =
                    tasks
                        .Where(x =>
                            x.Date == dateString)
                        .ToList();

                var dayShift =
                    shifts.FirstOrDefault(x =>
                        x.ShiftDate.Date == date.Date);

                VolunteerShiftDto? shiftDto = null;

                if (dayShift != null)
                {
                    shiftDto =
                        new VolunteerShiftDto
                        {
                            ShiftId =
                                dayShift
                                    .VolunteerRosterShiftId,

                            Date =
                                dayShift.ShiftDate.ToString(
                                    "yyyy-MM-dd"),

                            StartTime =
                                dayShift.StartTime.ToString(
                                    @"hh\:mm"),

                            EndTime =
                                dayShift.EndTime.ToString(
                                    @"hh\:mm"),

                            Status =
                                dayShift.Status.ToString(),

                            Notes =
                                dayShift.Notes
                        };
                }

                days.Add(
                    new VolunteerDayScheduleDto
                    {
                        Date =
                            dateString,

                        Shift =
                            shiftDto,

                        Tasks =
                            dayTasks
                    });
            }

            // ========================================================
            // RESULT
            //
            // IMPORTANT:
            // These properties are intentionally at ROOT level
            // because WeeklyTasksPage reads:
            //
            // decoded['startDate']
            // decoded['endDate']
            // decoded['days']
            //
            // ========================================================

            return Ok(new
            {
                success = true,

                startDate =
                    startDate.ToString(
                        "yyyy-MM-dd"),

                endDate =
                    endDate.ToString(
                        "yyyy-MM-dd"),

                totalTasks =
                    tasks.Count,

                days =
                    days
            });
        }

        // ============================================================
        // GET UPCOMING WEEK
        //
        // GET:
        // /api/VolunteerSchedule/upcoming-week
        //
        // The complete 7-day week AFTER the current week.
        //
        // Flutter:
        // UpcomingTasksPage
        // ============================================================

        [HttpGet("upcoming-week")]
        public async Task<IActionResult> GetUpcomingWeek(
            CancellationToken cancellationToken = default)
        {
            var volunteerId = GetVolunteerId();

            if (string.IsNullOrWhiteSpace(volunteerId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer identity could not be determined."
                });
            }

            var volunteerExists =
                await VolunteerExistsAsync(
                    volunteerId,
                    cancellationToken);

            if (!volunteerExists)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer account could not be found."
                });
            }

            var today = DateTime.Today;

            // ========================================================
            // CURRENT WEEK MONDAY
            // ========================================================

            var daysFromMonday =
                ((int)today.DayOfWeek + 6) % 7;

            var currentWeekStart =
                today.AddDays(-daysFromMonday);

            // ========================================================
            // NEXT WEEK MONDAY
            // ========================================================

            var startDate =
                currentWeekStart.AddDays(7);

            // Next week Sunday
            var endDate =
                startDate.AddDays(6);

            // Exclusive boundary
            var endDateExclusive =
                startDate.AddDays(7);

            // ========================================================
            // TASKS
            // ========================================================

            var taskEntities =
                await _context.VolunteerTaskAssignments
                    .AsNoTracking()
                    .Include(x => x.VolunteerTask)
                    .Where(x =>
                        x.VolunteerId == volunteerId &&
                        x.TaskDate >= startDate &&
                        x.TaskDate < endDateExclusive &&
                        x.Status !=
                            VolunteerTaskAssignmentStatus.Cancelled)
                    .OrderBy(x => x.TaskDate)
                    .ThenBy(x => x.StartTime)
                    .ToListAsync(cancellationToken);

            var tasks =
                taskEntities
                    .Select(x => new VolunteerTaskDto
                    {
                        TaskId =
                            x.VolunteerTaskAssignmentId,

                        Title =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Title
                                : string.Empty,

                        Description =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Description
                                : null,

                        Area =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Area.ToString()
                                : string.Empty,

                        Location =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Location
                                : null,

                        Date =
                            x.TaskDate.ToString(
                                "yyyy-MM-dd"),

                        StartTime =
                            x.StartTime.ToString(
                                @"hh\:mm"),

                        EndTime =
                            x.EndTime.ToString(
                                @"hh\:mm"),

                        Status =
                            x.Status.ToString(),

                        Priority =
                            x.VolunteerTask != null
                                ? x.VolunteerTask.Priority.ToString()
                                : string.Empty
                    })
                    .ToList();

            // ========================================================
            // SHIFTS
            // ========================================================

            var shifts =
                await _context.VolunteerRosterShifts
                    .AsNoTracking()
                    .Include(x => x.VolunteerRoster)
                    .Where(x =>
                        x.VolunteerRoster != null &&
                        x.VolunteerRoster.VolunteerId ==
                            volunteerId &&
                        x.ShiftDate >= startDate &&
                        x.ShiftDate < endDateExclusive &&
                        x.Status !=
                            VolunteerRosterShiftStatus.Cancelled)
                    .OrderBy(x => x.ShiftDate)
                    .ThenBy(x => x.StartTime)
                    .ToListAsync(cancellationToken);

            // ========================================================
            // BUILD 7 DAYS
            // ========================================================

            var days =
                new List<VolunteerDayScheduleDto>();

            for (var i = 0; i < 7; i++)
            {
                var date =
                    startDate.AddDays(i);

                var dateString =
                    date.ToString("yyyy-MM-dd");

                var dayTasks =
                    tasks
                        .Where(x =>
                            x.Date == dateString)
                        .ToList();

                var dayShift =
                    shifts.FirstOrDefault(x =>
                        x.ShiftDate.Date == date.Date);

                VolunteerShiftDto? shiftDto = null;

                if (dayShift != null)
                {
                    shiftDto =
                        new VolunteerShiftDto
                        {
                            ShiftId =
                                dayShift
                                    .VolunteerRosterShiftId,

                            Date =
                                dayShift.ShiftDate.ToString(
                                    "yyyy-MM-dd"),

                            StartTime =
                                dayShift.StartTime.ToString(
                                    @"hh\:mm"),

                            EndTime =
                                dayShift.EndTime.ToString(
                                    @"hh\:mm"),

                            Status =
                                dayShift.Status.ToString(),

                            Notes =
                                dayShift.Notes
                        };
                }

                days.Add(
                    new VolunteerDayScheduleDto
                    {
                        Date =
                            dateString,

                        Shift =
                            shiftDto,

                        Tasks =
                            dayTasks
                    });
            }



            // ========================================================
            // RESULT
            //
            // Matches UpcomingTasksPage:
            //
            // decoded['startDate']
            // decoded['endDate']
            // decoded['totalTasks']
            // decoded['days']
            //
            // ========================================================

            return Ok(new
            {
                success = true,

                startDate =
                    startDate.ToString(
                        "yyyy-MM-dd"),

                endDate =
                    endDate.ToString(
                        "yyyy-MM-dd"),

                totalTasks =
                    tasks.Count,

                days =
                    days
            });
        }

        // ============================================================
        // MONTH CALENDAR
        // ============================================================
        //
        // GET:
        // /api/VolunteerSchedule/month?year=2026&month=9
        //
        // Returns every day in the requested month.
        //
        // Each day can contain:
        // - Working shift
        // - Off day
        // - Tasks
        //
        // The volunteer is taken from the JWT.
        // The client cannot request another volunteer's schedule.
        //
        // ============================================================

        [HttpGet("month")]
        public async Task<IActionResult> GetMonthSchedule(
            [FromQuery] int? year = null,
            [FromQuery] int? month = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // ----------------------------------------------------
                // GET VOLUNTEER FROM JWT
                // ----------------------------------------------------

                var volunteerId = GetVolunteerId();

                if (string.IsNullOrWhiteSpace(volunteerId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Volunteer authentication is required."
                    });
                }

                // ----------------------------------------------------
                // VERIFY VOLUNTEER EXISTS
                // ----------------------------------------------------

                var volunteerExists =
                    await VolunteerExistsAsync(
                        volunteerId,
                        cancellationToken);

                if (!volunteerExists)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Volunteer account could not be found."
                    });
                }

                // ----------------------------------------------------
                // DETERMINE MONTH
                // ----------------------------------------------------

                var requestedYear =
                    year ?? DateTime.Today.Year;

                var requestedMonth =
                    month ?? DateTime.Today.Month;

                // ----------------------------------------------------
                // VALIDATE MONTH
                // ----------------------------------------------------

                if (requestedYear < 2000 ||
                    requestedYear > 2100)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid year."
                    });
                }

                if (requestedMonth < 1 ||
                    requestedMonth > 12)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Month must be between 1 and 12."
                    });
                }

                // ----------------------------------------------------
                // MONTH RANGE
                // ----------------------------------------------------

                var monthStart =
                    new DateTime(
                        requestedYear,
                        requestedMonth,
                        1,
                        0,
                        0,
                        0,
                        DateTimeKind.Unspecified);

                var nextMonthStart =
                    monthStart.AddMonths(1);

                // ----------------------------------------------------
                // GET ROSTER SHIFTS
                // ----------------------------------------------------
                //
                // We use the roster shift as the primary source for
                // determining whether the volunteer is working.
                //
                // Cancelled / DayOff shifts are not considered
                // working shifts.
                //
                // ----------------------------------------------------

                var shifts =
                    await _context.VolunteerRosterShifts
                        .AsNoTracking()
                        .Include(s => s.VolunteerRoster)
                        .Where(s =>
                            s.VolunteerRoster != null &&
                            s.VolunteerRoster.VolunteerId == volunteerId &&
                            s.ShiftDate >= monthStart &&
                            s.ShiftDate < nextMonthStart)
                        .OrderBy(s => s.ShiftDate)
                        .ThenBy(s => s.StartTime)
                        .ToListAsync(cancellationToken);

                // ----------------------------------------------------
                // GET TASK ASSIGNMENTS
                // ----------------------------------------------------

                var taskAssignments =
                    await _context.VolunteerTaskAssignments
                        .AsNoTracking()
                        .Include(a => a.VolunteerTask)
                        .Where(a =>
                            a.VolunteerId == volunteerId &&
                            a.TaskDate >= monthStart &&
                            a.TaskDate < nextMonthStart &&
                            a.Status != VolunteerTaskAssignmentStatus.Cancelled)
                        .OrderBy(a => a.TaskDate)
                        .ThenBy(a => a.StartTime)
                        .ToListAsync(cancellationToken);

                // ----------------------------------------------------
                // BUILD CALENDAR DAYS
                // ----------------------------------------------------

                var days = new List<VolunteerScheduleCalendarDayDto>();

                var daysInMonth =
                    DateTime.DaysInMonth(
                        requestedYear,
                        requestedMonth);

                for (var dayNumber = 1;
                     dayNumber <= daysInMonth;
                     dayNumber++)
                {
                    var currentDate =
                        monthStart.AddDays(dayNumber - 1);

                    // ------------------------------------------------
                    // SHIFTS FOR THIS DATE
                    // ------------------------------------------------

                    var dayShifts =
                        shifts
                            .Where(s =>
                                s.ShiftDate.Date ==
                                currentDate.Date)
                            .OrderBy(s => s.StartTime)
                            .ToList();

                    // ------------------------------------------------
                    // TASKS FOR THIS DATE
                    // ------------------------------------------------

                    var dayTasks =
                        taskAssignments
                            .Where(a =>
                                a.TaskDate.Date ==
                                currentDate.Date)
                            .OrderBy(a => a.StartTime)
                            .ToList();

                    // ------------------------------------------------
                    // FIND A SHIFT
                    // ------------------------------------------------
                    //
                    // Your current DTO has one Shift property.
                    //
                    // If multiple shifts exist on the same day,
                    // use the first scheduled one.
                    //
                    // ------------------------------------------------

                    var shift =
                        dayShifts
                            .FirstOrDefault(s =>
                                s.Status !=
                                    VolunteerRosterShiftStatus.Cancelled &&
                                s.Status !=
                                    VolunteerRosterShiftStatus.DayOff);

                    // ------------------------------------------------
                    // DETERMINE DAY STATUS
                    // ------------------------------------------------

                    string dayStatus;

                    if (shift != null)
                    {
                        dayStatus = "Working";
                    }
                    else if (dayShifts.Any(s =>
                        s.Status ==
                            VolunteerRosterShiftStatus.DayOff))
                    {
                        dayStatus = "Off";
                    }
                    else if (dayTasks.Any())
                    {
                        // A task assignment without a roster shift
                        // still means the volunteer has work scheduled.
                        dayStatus = "Working";
                    }
                    else
                    {
                        // Nothing has been assigned for this day.
                        dayStatus = "NoSchedule";
                    }

                    // ------------------------------------------------
                    // MAP SHIFT
                    // ------------------------------------------------

                    VolunteerCalendarShiftDto? shiftDto = null;

                    if (shift != null)
                    {
                        shiftDto =
                            new VolunteerCalendarShiftDto
                            {
                                ShiftId =
                                    shift.VolunteerRosterShiftId,

                                Date =
                                    shift.ShiftDate
                                        .ToString("yyyy-MM-dd"),

                                StartTime =
                                    shift.StartTime
                                        .ToString(@"hh\:mm\:ss"),

                                EndTime =
                                    shift.EndTime
                                        .ToString(@"hh\:mm\:ss"),

                                Status =
                                    shift.Status.ToString(),

                                Notes =
                                    shift.Notes
                            };
                    }

                    // ------------------------------------------------
                    // MAP TASKS
                    // ------------------------------------------------

                    var taskDtos =
                        dayTasks
                            .Where(a =>
                                a.VolunteerTask != null)
                            .Select(a =>
                                new VolunteerCalendarTaskDto
                                {
                                    TaskId =
                                        a.VolunteerTask!
                                            .VolunteerTaskId,

                                    Title =
                                        a.VolunteerTask!
                                            .Title,

                                    Description =
                                        a.VolunteerTask!
                                            .Description,

                                    Area =
                                        a.VolunteerTask!
                                            .Area
                                            .ToString(),

                                    Location =
                                        a.VolunteerTask!
                                            .Location,

                                    Date =
                                        a.TaskDate
                                            .ToString("yyyy-MM-dd"),

                                    StartTime =
                                        a.StartTime
                                            .ToString(@"hh\:mm\:ss"),

                                    EndTime =
                                        a.EndTime
                                            .ToString(@"hh\:mm\:ss"),

                                    Status =
                                        a.Status.ToString(),

                                    Priority =
                                        a.VolunteerTask!
                                            .Priority
                                            .ToString()
                                })
                            .ToList();

                    // ------------------------------------------------
                    // ADD DAY
                    // ------------------------------------------------

                    days.Add(
                        new VolunteerScheduleCalendarDayDto
                        {
                            Date =
                                currentDate
                                    .ToString("yyyy-MM-dd"),

                            Status =
                                dayStatus,

                            Shift =
                                shiftDto,

                            Tasks =
                                taskDtos
                        });
                }

                // ----------------------------------------------------
                // RETURN CALENDAR
                // ----------------------------------------------------

                return Ok(
                    new VolunteerScheduleCalendarDto
                    {
                        Success = true,

                        Year =
                            requestedYear,

                        Month =
                            requestedMonth,

                        Days =
                            days
                    });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading monthly volunteer schedule.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message =
                            "Unable to load the volunteer schedule."
                    });
            }
        }

        // ============================================================
        // GET TODAY'S SHIFT ONLY
        //
        // GET:
        // /api/VolunteerSchedule/shift
        //
        // Flutter can use this for a shift widget/card.
        // ============================================================

        [HttpGet("shift")]
        public async Task<IActionResult> GetTodayShift(
            CancellationToken cancellationToken = default)
        {
            var volunteerId = GetVolunteerId();

            if (string.IsNullOrWhiteSpace(volunteerId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer identity could not be determined."
                });
            }

            var volunteerExists =
                await VolunteerExistsAsync(
                    volunteerId,
                    cancellationToken);

            if (!volunteerExists)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer account could not be found."
                });
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // ========================================================
            // FIND TODAY'S SHIFT
            // ========================================================

            var shift =
                await _context.VolunteerRosterShifts
                    .AsNoTracking()
                    .Include(x => x.VolunteerRoster)
                    .Where(x =>
                        x.VolunteerRoster != null &&
                        x.VolunteerRoster.VolunteerId ==
                            volunteerId &&
                        x.ShiftDate >= today &&
                        x.ShiftDate < tomorrow &&
                        x.Status !=
                            VolunteerRosterShiftStatus.Cancelled)
                    .OrderBy(x => x.StartTime)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (shift == null)
            {
                return Ok(new
                {
                    success = true,
                    data = (VolunteerShiftDto?)null
                });
            }

            var result =
                new VolunteerShiftDto
                {
                    ShiftId =
                        shift.VolunteerRosterShiftId,

                    Date =
                        shift.ShiftDate.ToString(
                            "yyyy-MM-dd"),

                    StartTime =
                        shift.StartTime.ToString(
                            @"hh\:mm"),

                    EndTime =
                        shift.EndTime.ToString(
                            @"hh\:mm"),

                    Status =
                        shift.Status.ToString(),

                    Notes =
                        shift.Notes
                };

            return Ok(new
            {
                success = true,
                data = result
            });
        }

        // ============================================================
        // START TASK
        // ============================================================
        //
        // POST:
        // /api/VolunteerSchedule/tasks/{taskId}/start
        //
        // The taskId is the VolunteerTaskAssignmentId.
        //
        // Only the volunteer assigned to the task can start it.
        //
        // Allowed transition:
        //
        // Assigned -> InProgress
        //
        // ============================================================

        [HttpPost("tasks/{taskId}/start")]
        public async Task<IActionResult> StartTask(
            int taskId,
            CancellationToken cancellationToken = default)
        {
            var volunteerId = GetVolunteerId();

            if (string.IsNullOrWhiteSpace(volunteerId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer identity could not be determined."
                });
            }

            // ========================================================
            // FIND ASSIGNMENT
            // ========================================================

            var assignment =
                await _context.VolunteerTaskAssignments
                    .Include(x => x.VolunteerTask)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerTaskAssignmentId == taskId &&
                            x.VolunteerId == volunteerId,
                        cancellationToken);

            if (assignment == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "The assigned task could not be found."
                });
            }

            // ========================================================
            // CHECK CURRENT STATUS
            // ========================================================

            if (assignment.Status ==
                VolunteerTaskAssignmentStatus.Cancelled)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This task has been cancelled and cannot be started."
                });
            }

            if (assignment.Status ==
                VolunteerTaskAssignmentStatus.Missed)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This task has been marked as missed and cannot be started."
                });
            }

            if (assignment.Status ==
                VolunteerTaskAssignmentStatus.Completed)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This task has already been completed."
                });
            }

            if (assignment.Status ==
                VolunteerTaskAssignmentStatus.InProgress)
            {
                return Ok(new
                {
                    success = true,
                    message =
                        "This task is already in progress.",
                    taskId =
                        assignment.VolunteerTaskAssignmentId,
                    status =
                        assignment.Status.ToString(),
                    startedDate =
                        assignment.StartedDate
                });
            }

            // ========================================================
            // ONLY ASSIGNED CAN START
            // ========================================================

            if (assignment.Status !=
                VolunteerTaskAssignmentStatus.Assigned)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This task cannot be started from its current status."
                });
            }

            // ========================================================
            // START TASK
            // ========================================================

            var now = DateTime.UtcNow;

            assignment.Status =
                VolunteerTaskAssignmentStatus.InProgress;

            assignment.StartedDate = now;

            assignment.ModifiedDate = now;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Volunteer task started. AssignmentId={AssignmentId}, VolunteerId={VolunteerId}, StartedDate={StartedDate}",
                assignment.VolunteerTaskAssignmentId,
                volunteerId,
                now);

            // ========================================================
            // RESULT
            // ========================================================

            return Ok(new
            {
                success = true,

                message =
                    "Task started successfully.",

                taskId =
                    assignment.VolunteerTaskAssignmentId,

                status =
                    assignment.Status.ToString(),

                startedDate =
                    assignment.StartedDate
            });
        }


        // ============================================================
        // COMPLETE TASK
        // ============================================================
        //
        // POST:
        // /api/VolunteerSchedule/tasks/{taskId}/complete
        //
        // Body:
        //
        // {
        //     "completionNotes": "Task completed successfully."
        // }
        //
        // Allowed transition:
        //
        // InProgress -> Completed
        //
        // ============================================================

        [HttpPost("tasks/{taskId}/complete")]
        public async Task<IActionResult> CompleteTask(
            int taskId,
            [FromBody] CompleteVolunteerTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var volunteerId = GetVolunteerId();

            if (string.IsNullOrWhiteSpace(volunteerId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Volunteer identity could not be determined."
                });
            }

            // ========================================================
            // VALIDATE REQUEST
            // ========================================================

            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Completion data is required."
                });
            }

            var completionNotes =
                request.CompletionNotes?.Trim();

            if (!string.IsNullOrWhiteSpace(completionNotes) &&
                completionNotes.Length > 2000)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Completion notes cannot exceed 2000 characters."
                });
            }

            // ========================================================
            // FIND ASSIGNMENT
            // ========================================================

            var assignment =
                await _context.VolunteerTaskAssignments
                    .Include(x => x.VolunteerTask)
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerTaskAssignmentId == taskId &&
                            x.VolunteerId == volunteerId,
                        cancellationToken);

            if (assignment == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "The assigned task could not be found."
                });
            }

            // ========================================================
            // CHECK STATUS
            // ========================================================

            if (assignment.Status ==
                VolunteerTaskAssignmentStatus.Cancelled)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This task has been cancelled and cannot be completed."
                });
            }

            if (assignment.Status ==
                VolunteerTaskAssignmentStatus.Missed)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This task has been marked as missed and cannot be completed."
                });
            }

            if (assignment.Status ==
                VolunteerTaskAssignmentStatus.Completed)
            {
                return Ok(new
                {
                    success = true,
                    message =
                        "This task has already been completed.",
                    taskId =
                        assignment.VolunteerTaskAssignmentId,
                    status =
                        assignment.Status.ToString(),
                    completedDate =
                        assignment.CompletedDate,
                    completionNotes =
                        assignment.CompletionNotes
                });
            }

            // ========================================================
            // ONLY IN PROGRESS CAN BE COMPLETED
            // ========================================================

            if (assignment.Status !=
                VolunteerTaskAssignmentStatus.InProgress)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The task must be started before it can be completed."
                });
            }

            // ========================================================
            // COMPLETE TASK
            // ========================================================

            var now = DateTime.UtcNow;

            assignment.Status =
                VolunteerTaskAssignmentStatus.Completed;

            assignment.CompletedDate = now;

            assignment.CompletionNotes =
                string.IsNullOrWhiteSpace(completionNotes)
                    ? null
                    : completionNotes;

            assignment.ModifiedDate = now;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Volunteer task completed. AssignmentId={AssignmentId}, VolunteerId={VolunteerId}, CompletedDate={CompletedDate}",
                assignment.VolunteerTaskAssignmentId,
                volunteerId,
                now);

            // ========================================================
            // RESULT
            // ========================================================

            return Ok(new
            {
                success = true,

                message =
                    "Task completed successfully.",

                taskId =
                    assignment.VolunteerTaskAssignmentId,

                status =
                    assignment.Status.ToString(),

                startedDate =
                    assignment.StartedDate,

                completedDate =
                    assignment.CompletedDate,

                completionNotes =
                    assignment.CompletionNotes
            });
        }
    }
}