namespace swiftcare_api.Models.VolunteerCV
{
    public class CvExperience
    {
        public int CvExperienceId { get; set; }

        public int ParsedCvId { get; set; }

        public ParsedCv ParsedCv { get; set; } = null!;

        public string? Employer { get; set; }

        public string? Position { get; set; }

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public string? Description { get; set; }

        public string? RawText { get; set; }
    }
}