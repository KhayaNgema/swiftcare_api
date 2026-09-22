using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public class VolunteerOnboarding
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerOnboardingId { get; set; }

        // ============================================================
        // APPLICATION
        // ============================================================

        [Required]
        public int VolunteerApplicationId { get; set; }

        [Required]
        [StringLength(450)]
        public string ApplicantId { get; set; } = string.Empty;

        // ============================================================
        // STATUS
        // ============================================================

        [Required]
        public VolunteerOnboardingStatus Status { get; set; }
            = VolunteerOnboardingStatus.OfferPending;

        [Range(1, 20)]
        public int CurrentStep { get; set; } = 1;

        // ============================================================
        // OFFER
        // ============================================================

        public DateTime? OfferSentDate { get; set; }

        public DateTime? OfferAcceptedDate { get; set; }

        public DateTime? OfferDeclinedDate { get; set; }

        [StringLength(1000)]
        public string? OfferDeclineReason { get; set; }

        // ============================================================
        // POLICIES
        // ============================================================

        public DateTime? PoliciesAcceptedDate { get; set; }

        // ============================================================
        // CONTRACT
        // ============================================================

        public DateTime? ContractSubmittedDate { get; set; }

        // ============================================================
        // DOCUMENTS
        // ============================================================

        public DateTime? DocumentsSubmittedDate { get; set; }

        // ============================================================
        // ADMIN VERIFICATION
        // ============================================================

        public DateTime? VerificationStartedDate { get; set; }

        public DateTime? VerifiedDate { get; set; }

        [StringLength(450)]
        public string? VerifiedByAdminId { get; set; }

        [StringLength(1000)]
        public string? VerificationNotes { get; set; }

        // ============================================================
        // VOLUNTEER ACTIVATION
        // ============================================================

        public DateTime? VolunteerCreatedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        // ============================================================
        // AUDIT
        // ============================================================

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        [StringLength(450)]
        public string? ModifiedBy { get; set; }

        // ============================================================
        // NAVIGATION PROPERTIES
        // ============================================================

        public virtual VolunteerApplication VolunteerApplication { get; set; } = null!;

        //public virtual Applicant Applicant { get; set; } = null!;

        public virtual ICollection<VolunteerOffer> Offers { get; set; }
            = new List<VolunteerOffer>();

        public virtual ICollection<VolunteerPolicyAcceptance> PolicyAcceptances { get; set; }
            = new List<VolunteerPolicyAcceptance>();

        public virtual ICollection<VolunteerOnboardingDocument> Documents { get; set; }
            = new List<VolunteerOnboardingDocument>();

        public virtual ICollection<VolunteerOnboardingRequirement> Requirements { get; set; }
            = new List<VolunteerOnboardingRequirement>();

        public virtual Volunteer? Volunteer { get; set; }
    }
}