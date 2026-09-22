using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerApplication
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerApplicationId { get; set; }

        public DateTime AppliedDate { get; set; }
            = DateTime.Now;

        public VolunteerApplicationStatus Status { get; set; }
            = VolunteerApplicationStatus.Pending;

        [StringLength(2000)]
        public string? AdminNotes { get; set; }

        public DateTime? ReviewedDate { get; set; }

        public string? ReviewedById { get; set; }

        [StringLength(500)]
        public string? CVFilePath { get; set; }

        [StringLength(255)]
        public string? CVFileName { get; set; }

        public DateTime? CVUploadedDate { get; set; }

        public string ApplicantId { get; set; }

        [ForeignKey(nameof(ApplicantId))]
/*        public Applicant? Applicant { get; set; }*/

        public string CoverMessage { get; set; }


        [Required]
        [StringLength(30)]
        public string Reference { get; set; } = string.Empty;

        public int VolunteerOpportunityId { get; set; }

        [ForeignKey(nameof(VolunteerOpportunityId))]
        public VolunteerOpportunity? VolunteerOpportunity { get; set; }

           public ICollection<VolunteerApplicationAnswer> Answers { get; set; }
     = new List<VolunteerApplicationAnswer>();

        // ============================================================
        // ATS
        // ============================================================

        public VolunteerApplicationStage Stage { get; set; }
            = VolunteerApplicationStage.Applied;

        public VolunteerScreeningStatus ScreeningStatus { get; set; }
            = VolunteerScreeningStatus.NotScreened;

        public VolunteerEligibilityStatus EligibilityStatus { get; set; }
            = VolunteerEligibilityStatus.NotChecked;

        public VolunteerAIRecommendation AIRecommendation { get; set; }
            = VolunteerAIRecommendation.NotEvaluated;

        public VolunteerAdminDecision AdminDecision { get; set; }
            = VolunteerAdminDecision.NoDecision;

        public decimal? ATSScore { get; set; }

        public int? ATSRanking { get; set; }

        public DateTime? LastEvaluatedDate { get; set; }

        public DateTime? ShortlistedDate { get; set; }

        public DateTime? InterviewDate { get; set; }

        public DateTime? DecisionDate { get; set; }

        public VolunteerApplicationEvaluation? Evaluation { get; set; }
    }

    public enum VolunteerApplicationStatus
    {
        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Under Review")]
        UnderReview,

        [Display(Name = "Shortlisted")]
        Shortlisted,

        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Rejected")]
        Rejected,

        [Display(Name = "Withdrawn")]
        Withdrawn
    }
}