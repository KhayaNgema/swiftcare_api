namespace swiftcare_api.Models.VolunteerCV
{
    public class CvEducation
    {
        public int CvEducationId { get; set; }

        public int ParsedCvId { get; set; }

        public ParsedCv ParsedCv { get; set; } = null!;

        public string? Institution { get; set; }

        public string? Qualification { get; set; }

        public string? FieldOfStudy { get; set; }

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public string? RawText { get; set; }
    }
}