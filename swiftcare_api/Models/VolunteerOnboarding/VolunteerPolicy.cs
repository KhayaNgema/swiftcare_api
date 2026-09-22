using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public class VolunteerPolicy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerPolicyId { get; set; }

        // ============================================================
        // POLICY
        // ============================================================

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Version { get; set; } = "1.0";

        // ============================================================
        // DOCUMENT
        // ============================================================

        [StringLength(500)]
        public string? DocumentPath { get; set; }

        [StringLength(100)]
        public string? DocumentFileName { get; set; }

        // ============================================================
        // SETTINGS
        // ============================================================

        [Required]
        public bool IsRequired { get; set; } = true;

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? PublishedDate { get; set; }

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
        // NAVIGATION
        // ============================================================

        public virtual ICollection<VolunteerPolicyItem> Items { get; set; }
            = new List<VolunteerPolicyItem>();

        public virtual ICollection<VolunteerPolicyAcceptance> Acceptances { get; set; }
            = new List<VolunteerPolicyAcceptance>();
    }

        public class VolunteerPolicyItem
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int VolunteerPolicyItemId { get; set; }

            // ============================================================
            // RELATIONSHIP
            // ============================================================

            [Required]
            public int VolunteerPolicyId { get; set; }

            [ForeignKey(nameof(VolunteerPolicyId))]
            public virtual VolunteerPolicy VolunteerPolicy { get; set; } = null!;

            // ============================================================
            // CONTENT
            // ============================================================

            /*
             * Optional heading for this policy item.
             *
             * Example:
             *
             * "1.1 Respect for Residents"
             */
            [StringLength(300)]
            public string? Heading { get; set; }

            /*
             * Main content belonging to the heading.
             *
             * Example:
             *
             * "Residents must be treated as individuals..."
             */
            [Required]
            [StringLength(3000)]
            public string Description { get; set; } = string.Empty;

            /*
             * Indicates whether this item represents a visible
             * subheading followed by its description.
             */
            [Required]
            public bool IsSubheading { get; set; } = true;

            // ============================================================
            // DISPLAY ORDER
            // ============================================================

            [Required]
            public int DisplayOrder { get; set; }

            // ============================================================
            // SETTINGS
            // ============================================================

            [Required]
            public bool IsActive { get; set; } = true;

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
        }
    }
