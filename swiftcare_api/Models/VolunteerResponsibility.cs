using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerResponsibility
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerResponsibilityId { get; set; }

        [Required(ErrorMessage = "Responsibility is required.")]
        [StringLength(500, ErrorMessage = "Responsibility cannot exceed 500 characters.")]
        public string Description { get; set; }

        public int VolunteerOpportunityId { get; set; }

        [ForeignKey(nameof(VolunteerOpportunityId))]
        public VolunteerOpportunity? VolunteerOpportunity { get; set; }
    }
}