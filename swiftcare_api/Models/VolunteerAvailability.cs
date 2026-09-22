using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerAvailability
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerAvailabilityId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        public string VolunteerId { get; set; }

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }
    }
}