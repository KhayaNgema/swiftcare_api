namespace swiftcare_api.DTOs
{
    public class VolunteerWeekScheduleDto
    {
        public string StartDate { get; set; } = string.Empty;

        public string EndDate { get; set; } = string.Empty;

        public int TotalTasks { get; set; }

        public List<VolunteerDayScheduleDto> Days { get; set; }
            = new();
    }
}