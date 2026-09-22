using swiftcare_api.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerApplicationAnswer
    {
        [Key]
        public int VolunteerApplicationAnswerId { get; set; }

        public int VolunteerApplicationId { get; set; }

        [ForeignKey(nameof(VolunteerApplicationId))]
        public VolunteerApplication? VolunteerApplication { get; set; }

        public int VolunteerQuestionId { get; set; }

        [ForeignKey(nameof(VolunteerQuestionId))]
        public VolunteerQuestion? VolunteerQuestion { get; set; }

        [Required]
        [StringLength(3000)]
        public string Answer { get; set; } = string.Empty;
    }
}


