
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerTaskAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerTaskAssignmentId { get; set; }

        // =========================================================
        // TASK TEMPLATE
        // =========================================================

        /// <summary>
        /// The reusable task being assigned.
        /// Example: "Assist residents with morning exercise".
        /// </summary>
        [Required]
        public int VolunteerTaskId { get; set; }

        [ForeignKey(nameof(VolunteerTaskId))]
        public VolunteerTask? VolunteerTask { get; set; }

        // =========================================================
        // VOLUNTEER
        // =========================================================

        /// <summary>
        /// The volunteer who must perform this task.
        /// </summary>
        [Required]
        [StringLength(450)]
        public string VolunteerId { get; set; } = string.Empty;

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }

        // =========================================================
        // ROSTER
        // =========================================================

        /// <summary>
        /// Optional roster this assignment belongs to.
        /// A roster represents the volunteer's scheduled week.
        /// </summary>
        public int? VolunteerRosterId { get; set; }

        [ForeignKey(nameof(VolunteerRosterId))]
        public VolunteerRoster? VolunteerRoster { get; set; }

        // =========================================================
        // TASK DATE / TIME
        // =========================================================

        /// <summary>
        /// The specific day this task must be performed.
        /// </summary>
        [Required]
        public DateTime TaskDate { get; set; }

        /// <summary>
        /// Time the volunteer should start the task.
        /// </summary>
        [Required]
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Time the volunteer should finish the task.
        /// </summary>
        [Required]
        public TimeSpan EndTime { get; set; }

        // =========================================================
        // STATUS
        // =========================================================

        public VolunteerTaskAssignmentStatus Status { get; set; }
            = VolunteerTaskAssignmentStatus.Assigned;

        /// <summary>
        /// When the volunteer actually started the task.
        /// </summary>
        public DateTime? StartedDate { get; set; }

        /// <summary>
        /// When the volunteer completed the task.
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        // =========================================================
        // COMPLETION
        // =========================================================

        [StringLength(2000)]
        public string? CompletionNotes { get; set; }

        // =========================================================
        // ASSIGNED BY
        // =========================================================

        /// <summary>
        /// Administrator, supervisor, or staff member
        /// who created the assignment.
        /// </summary>
        [StringLength(450)]
        public string? AssignedById { get; set; }

        [ForeignKey(nameof(AssignedById))]
        public UserBaseModel? AssignedBy { get; set; }

        // =========================================================
        // AUDIT
        // =========================================================

        public DateTime CreatedDate { get; set; }
            = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }
    }

    public enum VolunteerTaskAssignmentStatus
    {
        [Display(Name = "Assigned")]
        Assigned = 1,

        [Display(Name = "In Progress")]
        InProgress = 2,

        [Display(Name = "Completed")]
        Completed = 3,

        [Display(Name = "Cancelled")]
        Cancelled = 4,

        [Display(Name = "Missed")]
        Missed = 5
    }
}