using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerQuestion
    {
        [Key]
        public int VolunteerQuestionId { get; set; }


        [Required]
        [StringLength(1000)]
        public string Question { get; set; } = string.Empty;

        public VolunteerQuestionType QuestionType { get; set; }
            = VolunteerQuestionType.LongAnswer;

        public bool IsRequired { get; set; } = true;


        public VolunteerCategory? Category { get; set; }


        public VolunteerArea? Area { get; set; }


        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }
    }


    public enum VolunteerQuestionType
    {
        [Display(Name = "Short Answer")]
        ShortAnswer,

        [Display(Name = "Long Answer")]
        LongAnswer,

        [Display(Name = "Yes / No")]
        YesNo
    }
}
    