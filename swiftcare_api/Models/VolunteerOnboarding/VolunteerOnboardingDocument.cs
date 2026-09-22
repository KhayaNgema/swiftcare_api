using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public class VolunteerOnboardingDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerOnboardingDocumentId { get; set; }

        // ============================================================
        // REFERENCES
        // ============================================================

        [Required]
        public int VolunteerOnboardingId { get; set; }

        [Required]
        [StringLength(450)]
        public string ApplicantId { get; set; } = string.Empty;

        // ============================================================
        // DOCUMENT TYPE
        // ============================================================

        [Required]
        public VolunteerOnboardingDocumentType DocumentType { get; set; }

        [Required]
        [StringLength(200)]
        public string DocumentName { get; set; } = string.Empty;

        // ============================================================
        // FILE INFORMATION
        // ============================================================

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Range(1, 52428800)]
        public long FileSize { get; set; }

        // ============================================================
        // STATUS
        // ============================================================

        [Required]
        public VolunteerOnboardingDocumentStatus Status { get; set; }
            = VolunteerOnboardingDocumentStatus.Uploaded;

        // ============================================================
        // UPLOAD
        // ============================================================

        [Required]
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        // ============================================================
        // ADMIN REVIEW
        // ============================================================

        public DateTime? ReviewedDate { get; set; }

        [StringLength(450)]
        public string? ReviewedByAdminId { get; set; }

        [StringLength(1000)]
        public string? RejectionReason { get; set; }

        // ============================================================
        // SECURITY / INTEGRITY
        // ============================================================

        [StringLength(128)]
        public string? FileHash { get; set; }

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