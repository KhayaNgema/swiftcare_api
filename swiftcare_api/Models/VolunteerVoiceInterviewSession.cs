using swiftcare_api.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace swiftcare_api.Models
{
    public class VolunteerVoiceInterviewSession
    {
        [Key]
        public int VolunteerVoiceInterviewSessionId { get; set; }

        public int VolunteerApplicationId { get; set; }

        [ForeignKey(nameof(VolunteerApplicationId))]
        public VolunteerApplication? VolunteerApplication { get; set; }

        [Required]
        [StringLength(100)]
        public string SessionToken { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? StartedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public int CurrentQuestionNumber { get; set; }

        public bool IsStarted { get; set; }

        public bool IsCompleted { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "NotStarted";
    }
}

