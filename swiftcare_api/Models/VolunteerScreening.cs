using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerScreening
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerScreeningId { get; set; }

        public VolunteerScreeningStatus Status { get; set; }
            = VolunteerScreeningStatus.NotScreened;

        public DateTime? ScreeningDate { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public string? CompletedById { get; set; }

        public string VolunteerId { get; set; }

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }
    }

    public enum VolunteerScreeningStatus
    {
        [Display(Name = "Not Screened")]
        NotScreened,

        [Display(Name = "Screening")]
        Screening,

        [Display(Name = "Screened")]
        Screened,

        [Display(Name = "Requires Review")]
        RequiresReview
    }
}