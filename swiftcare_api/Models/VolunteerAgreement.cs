using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerAgreement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerAgreementId { get; set; }

        [StringLength(200)]
        public string? AgreementName { get; set; }

        public bool Accepted { get; set; }

        public DateTime? AcceptedDate { get; set; }

        [StringLength(500)]
        public string? AgreementFilePath { get; set; }

        public string VolunteerId { get; set; }

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }
    }
}