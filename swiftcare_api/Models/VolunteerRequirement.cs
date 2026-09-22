using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerRequirement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerRequirementId { get; set; }

        [Required(ErrorMessage = "Requirement is required.")]
        [StringLength(500,
            ErrorMessage = "Requirement cannot exceed 500 characters.")]
        public string Description { get; set; }

        public int VolunteerOpportunityId { get; set; }

        [ForeignKey(nameof(VolunteerOpportunityId))]
        public VolunteerOpportunity? VolunteerOpportunity { get; set; }
    }
}