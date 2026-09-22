using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    /// <summary>
    /// AI assessment of one role-required skill.
    ///
    /// Example:
    ///
    /// Required Skill:
    /// Companionship
    ///
    /// CV:
    /// Found
    ///
    /// Interview:
    /// Demonstrated
    ///
    /// Score:
    /// 86
    ///
    /// Confidence:
    /// 91
    /// </summary>
    public class VolunteerInterviewSkillEvaluation
    {
        // ============================================================
        // PRIMARY KEY
        // ============================================================

        [Key]
        public int VolunteerInterviewSkillEvaluationId { get; set; }


        // ============================================================
        // PARENT EVALUATION
        // ============================================================

        [Required]
        public int VolunteerInterviewEvaluationId { get; set; }

        [ForeignKey(nameof(VolunteerInterviewEvaluationId))]
        public VolunteerInterviewEvaluation? VolunteerInterviewEvaluation { get; set; }


        // ============================================================
        // REQUIRED SKILL
        // ============================================================

        [Required]
        public int VolunteerRequiredSkillId { get; set; }

        [ForeignKey(nameof(VolunteerRequiredSkillId))]
        public VolunteerRequiredSkill? VolunteerRequiredSkill { get; set; }


        [Required]
        [StringLength(150)]
        public string Skill { get; set; } = string.Empty;


        // ============================================================
        // CV EVIDENCE
        // ============================================================

        public bool FoundInCv { get; set; }


        [StringLength(3000)]
        public string CvEvidence { get; set; } = string.Empty;


        // ============================================================
        // INTERVIEW EVIDENCE
        // ============================================================

        public bool FoundInInterview { get; set; }


        [StringLength(3000)]
        public string InterviewEvidence { get; set; } = string.Empty;


        // ============================================================
        // SCORE
        // ============================================================

        /// <summary>
        /// How strongly the candidate demonstrated the required skill.
        /// 0 - 100.
        /// </summary>
        [Range(0, 100)]
        public decimal InterviewScore { get; set; }


        /// <summary>
        /// AI confidence in the assessment.
        /// 0 - 100.
        /// </summary>
        [Range(0, 100)]
        public decimal Confidence { get; set; }


        // ============================================================
        // ASSESSMENT
        // ============================================================

        /// <summary>
        /// Demonstrated
        /// Partially Demonstrated
        /// Not Demonstrated
        /// Not Evaluated
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Assessment { get; set; } = "Not Evaluated";


        // ============================================================
        // COMBINED EVIDENCE
        // ============================================================

        [StringLength(5000)]
        public string Evidence { get; set; } = string.Empty;


        // ============================================================
        // AI REASONING
        // ============================================================

        [StringLength(5000)]
        public string Reasoning { get; set; } = string.Empty;
    }
}