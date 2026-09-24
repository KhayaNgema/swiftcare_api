namespace swiftcare_api.DTOs
{
    public class VolunteerTaskDto
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