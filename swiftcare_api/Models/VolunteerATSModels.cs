using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    // ============================================================
    // APPLICATION STAGE
    // ============================================================

    public enum VolunteerApplicationStage
    {
        [Display(Name = "Applied")]
        Applied,

        [Display(Name = "Eligibility Screening")]
        EligibilityScreening,

        [Display(Name = "AI Evaluation")]
        AIEvaluation,

        [Display(Name = "Shortlisted")]
        Shortlisted,

        [Display(Name = "Interview")]
        Interview,

        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Rejected")]
        Rejected
    }


    // ============================================================
    // ELIGIBILITY RESULT
    // ============================================================

    public enum VolunteerEligibilityStatus
    {
        [Display(Name = "Not Checked")]
        NotChecked,

        [Display(Name = "Eligible")]
        Eligible,

        [Display(Name = "Review Required")]
        ReviewRequired,

        [Display(Name = "Not Eligible")]
        NotEligible
    }


    // ============================================================
    // AI RECOMMENDATION
    // ============================================================

    public enum VolunteerAIRecommendation
    {
        [Display(Name = "Not Evaluated")]
        NotEvaluated,

        [Display(Name = "Strong Shortlist")]
        StrongShortlist,

        [Display(Name = "Review Recommended")]
        ReviewRecommended,

        [Display(Name = "Not Recommended")]
        NotRecommended
    }


    // ============================================================
    // ADMIN DECISION
    // ============================================================

    public enum VolunteerAdminDecision
    {
        [Display(Name = "No Decision")]
        NoDecision,

        [Display(Name = "Shortlist")]
        Shortlist,

        [Display(Name = "Interview")]
        Interview,

        [Display(Name = "Approve")]
        Approve,

        [Display(Name = "Reject")]
        Reject
    }


    // ============================================================
    // APPLICATION EVALUATION
    // ============================================================

    public class VolunteerApplicationEvaluation
    {
        [Key]
        public int VolunteerApplicationEvaluationId { get; set; }

        [Required]
        public int VolunteerApplicationId { get; set; }

        [ForeignKey(nameof(VolunteerApplicationId))]
        public VolunteerApplication? VolunteerApplication { get; set; }


        // --------------------------------------------------------
        // ELIGIBILITY
        // --------------------------------------------------------

        public VolunteerEligibilityStatus EligibilityStatus { get; set; }
            = VolunteerEligibilityStatus.NotChecked;

        [Range(0, 100)]
        public decimal EligibilityScore { get; set; }


        // --------------------------------------------------------
        // INDIVIDUAL SCORES
        // --------------------------------------------------------

        [Range(0, 100)]
        public decimal QualificationScore { get; set; }

        [Range(0, 100)]
        public decimal ExperienceScore { get; set; }

        [Range(0, 100)]
        public decimal SkillsScore { get; set; }

        [Range(0, 100)]
        public decimal QuestionScore { get; set; }

        [Range(0, 100)]
        public decimal AvailabilityScore { get; set; }

        [Range(0, 100)]
        public decimal MotivationScore { get; set; }


        // --------------------------------------------------------
        // FINAL SCORE
        // --------------------------------------------------------

        [Range(0, 100)]
        public decimal OverallScore { get; set; }


        // --------------------------------------------------------
        // RANK
        // --------------------------------------------------------

        public int? RankingPosition { get; set; }


        // --------------------------------------------------------
        // AI
        // --------------------------------------------------------

        public VolunteerAIRecommendation AIRecommendation { get; set; }
            = VolunteerAIRecommendation.NotEvaluated;

        [Range(0, 100)]
        public decimal AIConfidence { get; set; }


        [StringLength(5000)]
        public string? AIStrengths { get; set; }


        [StringLength(5000)]
        public string? AIConcerns { get; set; }


        [StringLength(10000)]
        public string? AIExplanation { get; set; }


        [StringLength(5000)]
        public string? AIRequirementMatches { get; set; }


        // --------------------------------------------------------
        // PROCESSING
        // --------------------------------------------------------

        public DateTime EvaluatedDate { get; set; }

        [StringLength(100)]
        public string? EvaluationVersion { get; set; }
    }


    // ============================================================
    // ATS AUDIT HISTORY
    // ============================================================

    public class VolunteerApplicationAudit
    {
        [Key]
        public int VolunteerApplicationAuditId { get; set; }


        [Required]
        public int VolunteerApplicationId { get; set; }

        [ForeignKey(nameof(VolunteerApplicationId))]
        public VolunteerApplication? VolunteerApplication { get; set; }


        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;


        [StringLength(1000)]
        public string? Description { get; set; }


        [StringLength(100)]
        public string? PerformedBy { get; set; }


        public DateTime CreatedDate { get; set; }
    }


    // ============================================================
    // OPPORTUNITY SCORING CONFIGURATION
    // ============================================================

    public class VolunteerOpportunityScoringConfiguration
    {
        [Key]
        public int VolunteerOpportunityScoringConfigurationId { get; set; }


        [Required]
        public int VolunteerOpportunityId { get; set; }

        [ForeignKey(nameof(VolunteerOpportunityId))]
        public VolunteerOpportunity? VolunteerOpportunity { get; set; }


        [Range(0, 100)]
        public decimal QualificationWeight { get; set; } = 25;


        [Range(0, 100)]
        public decimal ExperienceWeight { get; set; } = 20;


        [Range(0, 100)]
        public decimal SkillsWeight { get; set; } = 15;


        [Range(0, 100)]
        public decimal QuestionWeight { get; set; } = 20;


        [Range(0, 100)]
        public decimal AvailabilityWeight { get; set; } = 10;


        [Range(0, 100)]
        public decimal MotivationWeight { get; set; } = 10;


        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }
    }
}