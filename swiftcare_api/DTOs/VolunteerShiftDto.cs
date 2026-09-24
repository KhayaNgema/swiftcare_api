namespace swiftcare_api.DTOs
{
    public class VolunteerShiftDto
    {
        public int ShiftId { get; set; }

        public string Date { get; set; } = string.Empty;

        public string StartTime { get; set; } = string.Empty;

        public string EndTime { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}