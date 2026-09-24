using System.Collections.Generic;

namespace swiftcare_api.DTOs
{
    public class VolunteerScheduleCalendarDto
    {
        public bool Success { get; set; } = true;

        public int Year { get; set; }

        public int Month { get; set; }

        public List<VolunteerScheduleCalendarDayDto> Days { get; set; }
            = new();
    }

    public class VolunteerScheduleCalendarDayDto
    {
        public string Date { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public VolunteerCalendarShiftDto? Shift { get; set; }

        public List<VolunteerCalendarTaskDto> Tasks { get; set; }
            = new();
    }

    public class VolunteerCalendarShiftDto
    {
        public int ShiftId { get; set; }

        public string Date { get; set; } = string.Empty;

        public string StartTime { get; set; } = string.Empty;

        public string EndTime { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    public class VolunteerCalendarTaskDto
    {
        public int TaskId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Area { get; set; } = string.Empty;

        public string? Location { get; set; }

        public string Date { get; set; } = string.Empty;

        public string StartTime { get; set; } = string.Empty;

        public string EndTime { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;
    }
}