using swiftcare_api.Models.VolunteerCV;
using System.ComponentModel.DataAnnotations;

namespace swiftcare_api.Models
{
    public class Applicant : UserBaseModel
    {
        // ============================================================
        // PROFESSIONAL PROFILE
        // ============================================================

        [StringLength(100)]
        public string? ProfessionalTitle { get; set; }

        [StringLength(2000)]
        public string? ProfessionalSummary { get; set; }

        [StringLength(2000)]
        public string? CareerObjective { get; set; }


        // ============================================================
        // CONTACT / LOCATION
        // ============================================================

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }


        // ============================================================
        // PROFESSIONAL INFORMATION
        // ============================================================

        [StringLength(200)]
        public string? HighestQualification { get; set; }

        [StringLength(200)]
        public string? FieldOfStudy { get; set; }

        public int? YearsOfExperience { get; set; }


        // ============================================================
        // EMPLOYMENT PREFERENCES
        // ============================================================

        [StringLength(100)]
        public string? PreferredWorkType { get; set; }

        [StringLength(200)]
        public string? PreferredLocation { get; set; }

        public bool WillingToRelocate { get; set; }

        public bool AvailableForVolunteerWork { get; set; }


        // ============================================================
        // PROFESSIONAL LINKS
        // ============================================================

        [StringLength(500)]
        public string? LinkedInUrl { get; set; }

        [StringLength(500)]
        public string? PortfolioUrl { get; set; }

        [StringLength(500)]
        public string? GitHubUrl { get; set; }


        // ============================================================
        // CV INFORMATION
        // ============================================================

        [StringLength(500)]
        public string? CurrentCvFileName { get; set; }

        [StringLength(1000)]
        public string? CurrentCvFilePath { get; set; }

        [StringLength(50)]
        public string? CurrentCvFileType { get; set; }

        public DateTime? CvUploadedDate { get; set; }

        public DateTime? CvLastParsedDate { get; set; }

        public bool CvParsingSuccessful { get; set; }

        [StringLength(1000)]
        public string? CvParsingMessage { get; set; }


        // ============================================================
        // ATS PROFILE STATUS
        // ============================================================

        public bool IsProfileComplete { get; set; }

        public DateTime? ProfileLastUpdated { get; set; }


        // ============================================================
        // NAVIGATION
        // ============================================================

        public ICollection<ParsedCv> ParsedCvs { get; set; }
            = new List<ParsedCv>();
    }
}