using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using swiftcare_api.Models;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public class VolunteerIdentityVerification
    {
        public int Id { get; set; }

        // =========================================================
        // ONBOARDING / VOLUNTEER REFERENCES
        // =========================================================

        [Required]
        public int VolunteerOnboardingId { get; set; }

        public string? VolunteerId { get; set; }

        // The specific ID/passport document used for verification.
        public int? IdentityDocumentId { get; set; }


        // =========================================================
        // IDENTITY PROVIDER
        // =========================================================

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = "Didit";

        // Provider's verification/session/reference ID.
        [MaxLength(200)]
        public string? ProviderVerificationId { get; set; }

        // Provider's persistent person/identity reference.
        //
        // This is important because the same verified identity
        // should be reused for future mobile authentication rather
        // than creating another biometric identity.
        [MaxLength(200)]
        public string? ProviderPersonId { get; set; }


        // =========================================================
        // VERIFICATION STATUS
        // =========================================================

        [Required]
        public VolunteerIdentityVerificationStatus Status { get; set; }
            = VolunteerIdentityVerificationStatus.Pending;


        // =========================================================
        // VERIFICATION RESULTS
        // =========================================================

        // Was the submitted identity document successfully verified?
        public bool DocumentVerified { get; set; }

        // Did the provider confirm that the person was live?
        public bool LivenessVerified { get; set; }

        // Did the live face match the identity/document?
        public bool FaceMatchVerified { get; set; }

        // Indicates that the verified identity is available for
        // future authentication by the SwiftCare mobile application.
        public bool FaceIdentityEnrolled { get; set; }


        // =========================================================
        // DATES / AUDIT
        // =========================================================

        public DateTime? VerificationStartedDate { get; set; }

        public DateTime? VerifiedDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }


        // =========================================================
        // FAILURE / REVIEW INFORMATION
        // =========================================================

        [MaxLength(2000)]
        public string? FailureReason { get; set; }

        [MaxLength(2000)]
        public string? ReviewNotes { get; set; }

        [MaxLength(200)]
        public string? ProviderSessionId { get; set; }
        [MaxLength(2000)]
        public string? VerificationUrl { get; set; }
        // =========================================================
        // NAVIGATION PROPERTIES
        // =========================================================

        [ForeignKey(nameof(VolunteerOnboardingId))]
        public virtual VolunteerOnboarding VolunteerOnboarding { get; set; }
            = null!;

        [ForeignKey(nameof(VolunteerId))]
        public virtual Volunteer? Volunteer { get; set; }

        [ForeignKey(nameof(IdentityDocumentId))]
        public virtual VolunteerOnboardingDocument? IdentityDocument { get; set; }
    }


    // =============================================================
    // IDENTITY VERIFICATION STATUS
    // ===========================================================

    public enum VolunteerIdentityVerificationStatus
    {
        [Display(Name = "Pending")]
        Pending = 0,

        [Display(Name = "In Progress")]
        InProgress = 1,

        [Display(Name = "Awaiting User")]
        AwaitingUser = 2,

        [Display(Name = "Document Verified")]
        DocumentVerified = 3,

        [Display(Name = "Face Verification In Progress")]
        FaceVerificationInProgress = 4,

        [Display(Name = "Verified")]
        Verified = 5,

        [Display(Name = "Failed")]
        Failed = 6,

        [Display(Name = "Rejected")]
        Rejected = 7,

        [Display(Name = "Requires Review")]
        RequiresReview = 8,

        [Display(Name = "Expired")]
        Expired = 9,

        [Display(Name = "Cancelled")]
        Cancelled = 10
    }
}