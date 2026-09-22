using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerExperience
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerExperienceId { get; set; }

        [StringLength(150)]
        public string? Position { get; set; }

        [StringLength(200)]
        public string? Organization { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsCurrent { get; set; }

        public string VolunteerId { get; set; }

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }
    }
}