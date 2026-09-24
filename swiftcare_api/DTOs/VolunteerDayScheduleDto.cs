namespace swiftcare_api.DTOs
{
    public class VolunteerDayScheduleDto
    {
        public string Date { get; set; } = string.Empty;

        public VolunteerShiftDto? Shift { get; set; }

        public List<VolunteerTaskDto> Tasks { get; set; }
            = new List<VolunteerTaskDto>();
    }
}
