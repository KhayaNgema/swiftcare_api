using swiftcare_api.Models;
using System.ComponentModel.DataAnnotations.Schema;


namespace swiftcare_api.Models.VolunteerCV
{
    public class ParsedCv
    {
        // ============================================================
        // PRIMARY KEY
        // ============================================================

        public int ParsedCvId { get; set; }


        // ============================================================
        // APPLICANT RELATIONSHIP
        // ============================================================

        public string ApplicantId { get; set; }
        [ForeignKey(nameof(ApplicantId))]
        public Applicant Applicant { get; set; } = null!;


        // ============================================================
        // VOLUNTEER APPLICATION RELATIONSHIP
        // ============================================================

        public int? VolunteerApplicationId { get; set; }

        public VolunteerApplication? VolunteerApplication { get; set; }


        // ============================================================
        // PERSONAL / CONTACT INFORMATION
        // ============================================================

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }


        // ============================================================
        // PROFESSIONAL INFORMATION
        // ============================================================

        public string? ProfessionalTitle { get; set; }

        public string? ProfessionalSummary { get; set; }

        public string? CareerObjective { get; set; }


        // ============================================================
        // ADDRESS INFORMATION
        // ============================================================

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? Province { get; set; }

        public string? Country { get; set; }


        // ============================================================
        // ONLINE PROFILES
        // ============================================================

        public string? LinkedInUrl { get; set; }

        public string? PortfolioUrl { get; set; }

        public string? GitHubUrl { get; set; }


        // ============================================================
        // SKILLS
        // ============================================================

        public ICollection<CvSkill> Skills { get; set; }
            = new List<CvSkill>();


        // ============================================================
        // EDUCATION
        // ============================================================

        public ICollection<CvEducation> Education { get; set; }
            = new List<CvEducation>();


        // ============================================================
        // WORK EXPERIENCE
        // ============================================================

        public ICollection<CvExperience> Experience { get; set; }
            = new List<CvExperience>();


        // ============================================================
        // CERTIFICATIONS
        // ============================================================

        public ICollection<CvCertification> Certifications { get; set; }
            = new List<CvCertification>();


        // ============================================================
        // LANGUAGES
        // ============================================================

        public ICollection<CvLanguage> Languages { get; set; }
            = new List<CvLanguage>();


        // ============================================================
        // RAW / EXTRACTION INFORMATION
        // ============================================================

        public string? RawText { get; set; }

        public bool ExtractionSuccessful { get; set; }

        public string? ExtractionMessage { get; set; }


        // ============================================================
        // DATES
        // ============================================================

        public DateTime ParsedDate { get; set; }
            = DateTime.Now;
    }


        public class CvLanguage
        {
            public int CvLanguageId { get; set; }

            public int ParsedCvId { get; set; }

            public ParsedCv ParsedCv { get; set; } = null!;

            public string? Language { get; set; }

            public string? RawText { get; set; }
        }

        public class CvSkill
        {
            public int CvSkillId { get; set; }

            public int ParsedCvId { get; set; }

            public ParsedCv ParsedCv { get; set; } = null!;

            public string? Skill { get; set; }

            public string? RawText { get; set; }
        }
}