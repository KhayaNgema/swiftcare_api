using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using swiftcare_api.Models;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public class VolunteerContract
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerContractId { get; set; }


        // =========================================================
        // SOURCE RECORD REFERENCES
        // =========================================================

        [Required]
        public int VolunteerOnboardingId { get; set; }

        [Required]
        public int VolunteerApplicationId { get; set; }

        [Required]
        public int VolunteerOpportunityId { get; set; }

        [Required]
        public int VolunteerOfferId { get; set; }

        [Required]
        [StringLength(450)]
        public string ApplicantId { get; set; } = string.Empty;


        // =========================================================
        // CONTRACT INFORMATION
        // =========================================================

        [Required]
        [StringLength(50)]
        public string ContractReference { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ContractVersion { get; set; } = "1.0";


        // =========================================================
        // POSITION SNAPSHOT
        // =========================================================

        /// <summary>
        /// Historical snapshot of the volunteer opportunity title
        /// at the time the contract was generated.
        /// </summary>
        [Required]
        [StringLength(150)]
        public string PositionTitle { get; set; } = string.Empty;

        /// <summary>
        /// Historical snapshot of the opportunity description.
        /// </summary>
        [StringLength(3000)]
        public string? RoleDescription { get; set; }

        /// <summary>
        /// Historical snapshot of the volunteer category.
        /// </summary>
        public VolunteerCategory? Category { get; set; }

        /// <summary>
        /// Historical snapshot of the volunteer area.
        /// </summary>
        public VolunteerArea? Area { get; set; }

        /// <summary>
        /// Historical snapshot of the opportunity city.
        /// </summary>
        [StringLength(150)]
        public string? CityName { get; set; }

        /// <summary>
        /// Historical snapshot of the opportunity province.
        /// </summary>
        [StringLength(150)]
        public string? Province { get; set; }

        /// <summary>
        /// Historical snapshot of the volunteer start date.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Historical snapshot of the volunteer end date.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Historical snapshot of the volunteer start time.
        /// </summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>
        /// Historical snapshot of the volunteer end time.
        /// </summary>
        public TimeSpan? EndTime { get; set; }

        /// <summary>
        /// Historical snapshot of the minimum volunteer age.
        /// </summary>
        public int? MinimumAge { get; set; }


        // =========================================================
        // HISTORICAL OPPORTUNITY SNAPSHOTS
        // =========================================================

        /// <summary>
        /// Historical snapshot of the opportunity requirements.
        ///
        /// Each requirement is stored as a separate line.
        /// The actual VolunteerRequirement model is converted into
        /// readable text when the contract is created.
        /// </summary>
        [StringLength(10000)]
        public string? RequirementsSnapshot { get; set; }

        /// <summary>
        /// Historical snapshot of the opportunity responsibilities.
        ///
        /// Each responsibility is stored as a separate line.
        /// The actual VolunteerResponsibility model is converted into
        /// readable text when the contract is created.
        /// </summary>
        [StringLength(10000)]
        public string? ResponsibilitiesSnapshot { get; set; }

        /// <summary>
        /// Historical snapshot of the required skills.
        ///
        /// Each VolunteerRequiredSkill.Skill value is stored as a
        /// separate line.
        /// </summary>
        [StringLength(10000)]
        public string? RequiredSkillsSnapshot { get; set; }

        /// <summary>
        /// Historical snapshot of any additional contract clauses
        /// applicable to the volunteer opportunity.
        /// </summary>
        [StringLength(10000)]
        public string? AdditionalClausesSnapshot { get; set; }


        // =========================================================
        // CONTRACT STATUS
        // =========================================================

        [Required]
        public VolunteerContractStatus Status { get; set; }
            = VolunteerContractStatus.Generated;


        // =========================================================
        // GENERATED CONTRACT
        // =========================================================

        /// <summary>
        /// Date and time on which the PDF contract was generated.
        /// </summary>
        public DateTime? GeneratedDate { get; set; }

        /// <summary>
        /// Generated PDF file name.
        /// </summary>
        [StringLength(255)]
        public string? GeneratedFileName { get; set; }

        /// <summary>
        /// Relative web path to the generated PDF.
        /// Example:
        /// /UploadedFiles/VolunteerContracts/Generated/SC-CON-xxxx.pdf
        /// </summary>
        [StringLength(500)]
        public string? GeneratedFilePath { get; set; }


        // =========================================================
        // SIGNED CONTRACT
        // =========================================================

        /// <summary>
        /// Date and time on which the volunteer signed the contract.
        /// </summary>
        public DateTime? SignedDate { get; set; }

        /// <summary>
        /// Signed contract file name.
        /// </summary>
        [StringLength(255)]
        public string? SignedFileName { get; set; }

        /// <summary>
        /// Relative web path to the signed contract.
        /// </summary>
        [StringLength(500)]
        public string? SignedFilePath { get; set; }


        // =========================================================
        // ADMIN REVIEW
        // =========================================================

        /// <summary>
        /// Date and time on which the contract was reviewed.
        /// </summary>
        public DateTime? ReviewedDate { get; set; }

        /// <summary>
        /// Administrator who reviewed the contract.
        /// </summary>
        [StringLength(450)]
        public string? ReviewedByAdminId { get; set; }

        /// <summary>
        /// Reason supplied when the contract is rejected or requires
        /// resubmission.
        /// </summary>
        [StringLength(2000)]
        public string? RejectionReason { get; set; }


        // =========================================================
        // AUDIT
        // =========================================================

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        [StringLength(450)]
        public string? ModifiedBy { get; set; }


        // =========================================================
        // NAVIGATION PROPERTIES
        // =========================================================

        [ForeignKey(nameof(VolunteerOnboardingId))]
        public virtual VolunteerOnboarding VolunteerOnboarding { get; set; }
            = null!;

        [ForeignKey(nameof(VolunteerApplicationId))]
        public virtual VolunteerApplication VolunteerApplication { get; set; }
            = null!;

        [ForeignKey(nameof(VolunteerOpportunityId))]
        public virtual VolunteerOpportunity VolunteerOpportunity { get; set; }
            = null!;

        [ForeignKey(nameof(VolunteerOfferId))]
        public virtual VolunteerOffer VolunteerOffer { get; set; }
            = null!;
    }

    public enum VolunteerContractStatus
    {
        [Display(Name = "Not Generated")]
        NotGenerated = 0,

        [Display(Name = "Generated")]
        Generated = 1,

        [Display(Name = "Awaiting Applicant")]
        AwaitingApplicant = 2,

        [Display(Name = "Submitted")]
        Submitted = 3,

        [Display(Name = "Under Review")]
        UnderReview = 4,

        [Display(Name = "Correction Required")]
        CorrectionRequired = 5,

        [Display(Name = "Approved")]
        Approved = 6,

        [Display(Name = "Rejected")]
        Rejected = 7
    }
}
