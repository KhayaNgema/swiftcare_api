using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public class VolunteerOffer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerOfferId { get; set; }

        // ============================================================
        // REFERENCES
        // ============================================================

        [Required]
        public int VolunteerOnboardingId { get; set; }

        [Required]
        public int VolunteerApplicationId { get; set; }

        [Required]
        [StringLength(450)]
        public string ApplicantId { get; set; } = string.Empty;

        // ============================================================
        // OFFER INFORMATION
        // ============================================================

        [Required]
        [StringLength(50)]
        public string OfferReference { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string PositionTitle { get; set; } = string.Empty;

        [Required]
        public int VolunteerOpportunityId { get; set; }

        // ============================================================
        // DATES
        // ============================================================

        [Required]
        public DateTime IssuedDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime ExpiryDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        public DateTime? DeclinedDate { get; set; }

        // ============================================================
        // STATUS
        // ============================================================

        [Required]
        public VolunteerOfferStatus Status { get; set; }
            = VolunteerOfferStatus.Pending;

        [StringLength(1000)]
        public string? DeclineReason { get; set; }

        // ============================================================
        // SECURE ACCEPTANCE TOKEN
        // ============================================================

        [Required]
        [StringLength(128)]
        public string AcceptanceTokenHash { get; set; } = string.Empty;

        [Required]
        public DateTime TokenExpiryDate { get; set; }

        // ============================================================
        // ADMIN
        // ============================================================

        [StringLength(450)]
        public string? CreatedByAdminId { get; set; }

        // ============================================================
        // AUDIT
        // ============================================================

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        // ============================================================
        // NAVIGATION
        // ============================================================

        public virtual VolunteerOnboarding VolunteerOnboarding { get; set; }
            = null!;

        public virtual VolunteerApplication VolunteerApplication { get; set; }
            = null!;

        public virtual VolunteerOpportunity VolunteerOpportunity { get; set; }
            = null!;
    }
}