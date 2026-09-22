using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerRequiredSkill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerRequiredSkillId { get; set; }

        [Required(ErrorMessage = "Skill is required.")]
        [StringLength(150,
            ErrorMessage = "Skill cannot exceed 150 characters.")]
        public string Skill { get; set; }

        public int VolunteerOpportunityId { get; set; }

        [ForeignKey(nameof(VolunteerOpportunityId))]
        public VolunteerOpportunity? VolunteerOpportunity { get; set; }
    }
}