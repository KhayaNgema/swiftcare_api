using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerRosterShift
    {
        // ============================================================
        // PRIMARY KEY
        // ============================================================

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerRosterShiftId { get; set; }


        // ============================================================
        // ROSTER
        // ============================================================

        [Required]
        public int VolunteerRosterId { get; set; }

        [ForeignKey(nameof(VolunteerRosterId))]
        public VolunteerRoster? VolunteerRoster { get; set; }


        // ============================================================
        // SHIFT DATE
        // ============================================================

        /// <summary>
        /// The specific calendar date of this shift.
        /// </summary>
        [Required]
        public DateTime ShiftDate { get; set; }


        // ============================================================
        // SHIFT TIMES
        // ============================================================

        /// <summary>
        /// Scheduled start time for this specific day.
        /// </summary>
        [Required]
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Scheduled end time for this specific day.
        /// </summary>
        [Required]
        public TimeSpan EndTime { get; set; }


        // ============================================================
        // SHIFT STATUS
        // ============================================================

        [Required]
        public VolunteerRosterShiftStatus Status { get; set; }
            = VolunteerRosterShiftStatus.Scheduled;


        // ============================================================
        // NOTES
        // ============================================================

        [StringLength(1000)]
        public string? Notes { get; set; }


        // ============================================================
        // AUDIT
        // ============================================================

        public DateTime CreatedDate { get; set; }
            = DateTime.Now;

        public DateTime ModifiedDate { get; set; }
            = DateTime.Now;


        // ============================================================
        // ATTENDANCE
        // ============================================================

        /// <summary>
        /// Attendance recorded against this specific shift.
        /// </summary>
        public VolunteerAttendance? Attendance { get; set; }
    }


    // ================================================================
    // SHIFT STATUS
    // ================================================================

    public enum VolunteerRosterShiftStatus
    {
        [Display(Name = "Scheduled")]
        Scheduled = 1,

        [Display(Name = "Completed")]
        Completed = 2,

        [Display(Name = "Cancelled")]
        Cancelled = 3,

        [Display(Name = "Day Off")]
        DayOff = 4
    }
}