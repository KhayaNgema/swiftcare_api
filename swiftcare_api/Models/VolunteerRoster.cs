using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerRoster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerRosterId { get; set; }


        // ============================================================
        // ROSTER INFORMATION
        // ============================================================

        [Required]
        [StringLength(200)]
        public string RosterName { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// The first day of the seven-day roster period.
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The last day of the seven-day roster period.
        /// </summary>
        [Required]
        public DateTime EndDate { get; set; }

        public VolunteerRosterStatus Status { get; set; }
            = VolunteerRosterStatus.Draft;

        [StringLength(2000)]
        public string? Notes { get; set; }


        // ============================================================
        // VOLUNTEER
        // ============================================================

        [Required]
        [StringLength(450)]
        public string VolunteerId { get; set; } = string.Empty;

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }


        // ============================================================
        // SUPERVISOR
        // ============================================================

        [Required]
        [StringLength(450)]
        public string SupervisorId { get; set; } = string.Empty;

        [ForeignKey(nameof(SupervisorId))]
        public Supervisor? Supervisor { get; set; }


        // ============================================================
        // AUDIT
        // ============================================================

        public DateTime CreatedDate { get; set; }
            = DateTime.Now;

        public DateTime ModifiedDate { get; set; }
            = DateTime.Now;


        // ============================================================
        // TASK ASSIGNMENTS
        // ============================================================

        public ICollection<VolunteerTaskAssignment> TaskAssignments { get; set; }
            = new List<VolunteerTaskAssignment>();


        // ============================================================
        // DAILY SHIFTS
        // ============================================================

        /// <summary>
        /// Individual scheduled shifts belonging to this roster.
        ///
        /// A roster can contain different times for different days.
        /// </summary>
        public ICollection<VolunteerRosterShift> Shifts { get; set; }
            = new List<VolunteerRosterShift>();
    }


    // ================================================================
    // ROSTER STATUS
    // ================================================================

    public enum VolunteerRosterStatus
    {
        [Display(Name = "Draft")]
        Draft = 1,

        [Display(Name = "Published")]
        Published = 2,

        [Display(Name = "Active")]
        Active = 3,

        [Display(Name = "Completed")]
        Completed = 4,

        [Display(Name = "Cancelled")]
        Cancelled = 5
    }
}