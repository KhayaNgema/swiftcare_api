using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public class VolunteerOnboardingRequirement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerOnboardingRequirementId { get; set; }

        // ============================================================
        // ONBOARDING
        // ============================================================

        [Required]
        public int VolunteerOnboardingId { get; set; }

        // ============================================================
        // REQUIREMENT
        // ============================================================

        [Required]
        public VolunteerOnboardingRequirementType RequirementType { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        // ============================================================
        // STATUS
        // ============================================================

        [Required]
        public bool IsRequired { get; set; } = true;

        [Required]
        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedDate { get; set; }

        [StringLength(450)]
        public string? CompletedByAdminId { get; set; }

        // ============================================================
        // DISPLAY
        // ============================================================

        [Range(1, 100)]
        public int DisplayOrder { get; set; }

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
    }
}