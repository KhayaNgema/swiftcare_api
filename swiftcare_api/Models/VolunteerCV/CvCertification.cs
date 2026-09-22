namespace swiftcare_api.Models.VolunteerCV
{
    public class CvCertification
    {
        public int CvCertificationId { get; set; }

        public int ParsedCvId { get; set; }

        public ParsedCv ParsedCv { get; set; } = null!;

        public string? Name { get; set; }

        public string? IssuingOrganisation { get; set; }

        public string? Date { get; set; }

        public string? RawText { get; set; }
    }
}