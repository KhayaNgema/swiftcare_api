using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerDocumentId { get; set; }

        [StringLength(150)]
        public string? DocumentName { get; set; }

        [StringLength(500)]
        public string? FilePath { get; set; }

        public DateTime? UploadedDate { get; set; }

        public bool IsVerified { get; set; }

        public DateTime? VerifiedDate { get; set; }

        public string? VerifiedById { get; set; }

        public string ApplicantId { get; set; }
    } 
}