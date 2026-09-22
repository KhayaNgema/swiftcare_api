using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public class VolunteerPolicyAcceptance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerPolicyAcceptanceId { get; set; }

        // ============================================================
        // REFERENCES
        // ============================================================

        [Required]
        public int VolunteerOnboardingId { get; set; }

        [Required]
        public int VolunteerPolicyId { get; set; }

        [Required]
        [StringLength(450)]
        public string ApplicantId { get; set; } = string.Empty;

        // ============================================================
        // ACCEPTANCE
        // ============================================================

        [Required]
        public bool Accepted { get; set; }

        public DateTime? AcceptedDate { get; set; }

        // ============================================================
        // SNAPSHOT OF VERSION ACCEPTED
        // ============================================================

        [Required]
        [StringLength(30)]
        public string PolicyVersion { get; set; } = string.Empty;

        // ============================================================
        // OPTIONAL AUDIT INFORMATION
        // ============================================================

        [StringLength(100)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        // ============================================================
        // AUDIT
        // ============================================================

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // ============================================================
        // NAVIGATION
        // ============================================================

        public virtual VolunteerOnboarding VolunteerOnboarding { get; set; }
            = null!;

        public virtual VolunteerPolicy VolunteerPolicy { get; set; }
            = null!;
    }
}