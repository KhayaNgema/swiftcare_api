using swiftcare_api.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerOpportunityQuestion
    {
        [Key]
        public int VolunteerOpportunityQuestionId { get; set; }

        public int VolunteerOpportunityId { get; set; }

        [ForeignKey(nameof(VolunteerOpportunityId))]
        public VolunteerOpportunity? VolunteerOpportunity { get; set; }

        public int VolunteerQuestionId { get; set; }

        [ForeignKey(nameof(VolunteerQuestionId))]
        public VolunteerQuestion? VolunteerQuestion { get; set; }


        public int DisplayOrder { get; set; }
    }
}