
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace swiftcare_api.Models
    {
        public class VolunteerTask
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int VolunteerTaskId { get; set; }

            // =========================================================
            // TASK INFORMATION
            // =========================================================

            [Required]
            [StringLength(200)]
            public string Title { get; set; } = string.Empty;

            [StringLength(2000)]
            public string? Description { get; set; }

            // Area the task belongs to
            [Required]
            public VolunteerArea Area { get; set; }

            // Optional default location
            [StringLength(200)]
            public string? Location { get; set; }

            // Default priority for this task
            public VolunteerTaskPriority Priority { get; set; }
                = VolunteerTaskPriority.Normal;

            // Whether this task is available for assignment
            public bool IsActive { get; set; } = true;

            // =========================================================
            // OPTIONAL VOLUNTEER OPPORTUNITY
            // =========================================================

            public int? VolunteerOpportunityId { get; set; }

            [ForeignKey(nameof(VolunteerOpportunityId))]
            public VolunteerOpportunity? VolunteerOpportunity { get; set; }

            // =========================================================
            // ASSIGNMENTS
            // =========================================================

            public ICollection<VolunteerTaskAssignment> Assignments { get; set; }
                = new List<VolunteerTaskAssignment>();
        }


        public enum VolunteerTaskPriority
        {
            [Display(Name = "Low")]
            Low = 1,

            [Display(Name = "Normal")]
            Normal = 2,

            [Display(Name = "High")]
            High = 3,

            [Display(Name = "Urgent")]
            Urgent = 4
        }
    }
