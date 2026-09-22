using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerSkill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerSkillId { get; set; }

        [StringLength(150)]
        public string? Skill { get; set; }

        public string VolunteerId { get; set; }

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }
    }
}