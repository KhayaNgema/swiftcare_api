using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    /// <summary>
    /// AI assessment of a personal / behavioural competency
    /// demonstrated during the volunteer interview.
    /// </summary>
    public class VolunteerInterviewCompetencyEvaluation
    {
        // ============================================================
        // PRIMARY KEY
        // ============================================================

        [Key]
        public int VolunteerInterviewCompetencyEvaluationId { get; set; }


        // ============================================================
        // PARENT EVALUATION
        // ============================================================

        [Required]
        public int VolunteerInterviewEvaluationId { get; set; }

        [ForeignKey(nameof(VolunteerInterviewEvaluationId))]
        public VolunteerInterviewEvaluation? VolunteerInterviewEvaluation { get; set; }


        // ============================================================
        // COMPETENCY
        // ============================================================

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;


        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;


        // ============================================================
        // SCORE
        // ============================================================

        /// <summary>
        /// Competency score from 0 to 100.
        /// </summary>
        [Range(0, 100)]
        public decimal Score { get; set; }


        /// <summary>
        /// AI confidence from 0 to 100.
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
        // EVIDENCE
        // ============================================================

        /// <summary>
        /// Specific evidence from the candidate's interview answers.
        /// </summary>
        [StringLength(5000)]
        public string Evidence { get; set; } = string.Empty;


        // ============================================================
        // AI REASONING
        // ============================================================

        [StringLength(5000)]
        public string Reasoning { get; set; } = string.Empty;


        // ============================================================
        // DEMONSTRATED
        // ============================================================

        public bool Demonstrated { get; set; }
    }
}