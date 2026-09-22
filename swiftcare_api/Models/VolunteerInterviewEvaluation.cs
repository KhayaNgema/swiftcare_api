using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    /// <summary>
    /// Stores the complete AI evaluation of a completed volunteer voice interview.
    ///
    /// One application can have one current interview evaluation.
    /// The evaluation contains:
    /// - overall interview scoring
    /// - recommendation
    /// - confidence
    /// - role-required skill assessments
    /// - personal/behavioural competency assessments
    /// - Foundry / AI metadata
    /// </summary>
    public class VolunteerInterviewEvaluation
    {
        // ============================================================
        // PRIMARY KEY
        // ============================================================

        [Key]
        public int VolunteerInterviewEvaluationId { get; set; }


        // ============================================================
        // APPLICATION
        // ============================================================

        [Required]
        public int VolunteerApplicationId { get; set; }

        [ForeignKey(nameof(VolunteerApplicationId))]
        public VolunteerApplication? VolunteerApplication { get; set; }


        // ============================================================
        // INTERVIEW SESSION
        // ============================================================

        public int? VolunteerVoiceInterviewSessionId { get; set; }

        [ForeignKey(nameof(VolunteerVoiceInterviewSessionId))]
        public VolunteerVoiceInterviewSession? VolunteerVoiceInterviewSession { get; set; }


        // ============================================================
        // OVERALL INTERVIEW RESULT
        // ============================================================

        /// <summary>
        /// Overall interview score from 0 to 100.
        /// </summary>
        [Range(0, 100)]
        public decimal OverallInterviewScore { get; set; }


        /// <summary>
        /// AI confidence from 0 to 100.
        /// </summary>
        [Range(0, 100)]
        public decimal OverallInterviewConfidence { get; set; }


        /// <summary>
        /// StrongShortlist / ReviewRecommended / NotRecommended.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Recommendation { get; set; } = "Not Evaluated";


        /// <summary>
        /// Evaluated / Not Evaluated / Failed.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string EvaluationStatus { get; set; } = "Not Evaluated";


        /// <summary>
        /// Human-readable explanation of the final interview assessment.
        /// </summary>
        [StringLength(5000)]
        public string Explanation { get; set; } = string.Empty;


        // ============================================================
        // EVALUATION METADATA
        // ============================================================

        [Required]
        [StringLength(100)]
        public string EvaluationVersion { get; set; } = "1.0";


        /// <summary>
        /// Engine/provider used for the evaluation.
        /// Examples:
        /// RuleBased
        /// AzureOpenAI
        /// Foundry
        /// FoundryRubric
        /// </summary>
        [StringLength(100)]
        public string EvaluationProvider { get; set; } = "SwiftCare";


        /// <summary>
        /// Specific AI model used when applicable.
        /// </summary>
        [StringLength(200)]
        public string EvaluationModel { get; set; } = string.Empty;


        /// <summary>
        /// Foundry evaluation/run identifier when Foundry is used.
        /// </summary>
        [StringLength(200)]
        public string FoundryEvaluationId { get; set; } = string.Empty;


        /// <summary>
        /// Optional identifier for the evaluation dataset/version.
        /// </summary>
        [StringLength(200)]
        public string EvaluationDatasetVersion { get; set; } = string.Empty;


        /// <summary>
        /// Optional hash/version of the evaluation prompt or rubric.
        /// </summary>
        [StringLength(200)]
        public string EvaluationPromptVersion { get; set; } = string.Empty;


        // ============================================================
        // RAW / STRUCTURED AI RESULT
        // ============================================================

        /// <summary>
        /// Optional JSON representation of the AI evaluation response.
        ///
        /// Useful for auditing/debugging and later Foundry integration.
        /// </summary>
        public string EvaluationPayloadJson { get; set; } = string.Empty;


        /// <summary>
        /// Error message if the evaluation failed.
        /// </summary>
        [StringLength(4000)]
        public string ErrorMessage { get; set; } = string.Empty;


        // ============================================================
        // DATES
        // ============================================================

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? EvaluatedDate { get; set; }

        public DateTime? LastUpdatedDate { get; set; }


        // ============================================================
        // CHILD EVALUATIONS
        // ============================================================

        public ICollection<VolunteerInterviewSkillEvaluation>
            SkillEvaluations
        { get; set; }
            = new List<VolunteerInterviewSkillEvaluation>();


        public ICollection<VolunteerInterviewCompetencyEvaluation>
            CompetencyEvaluations
        { get; set; }
            = new List<VolunteerInterviewCompetencyEvaluation>();
    }
}