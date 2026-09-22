using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerAttendance
    {
        // ============================================================
        // PRIMARY KEY
        // ============================================================

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttendanceId { get; set; }


        // ============================================================
        // VOLUNTEER
        // ============================================================

        [Required]
        [StringLength(450)]
        public string VolunteerId { get; set; } = string.Empty;

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }


        // ============================================================
        // ROSTER SHIFT
        // ============================================================

        [Required]
        public int VolunteerRosterShiftId { get; set; }

        [ForeignKey(nameof(VolunteerRosterShiftId))]
        public VolunteerRosterShift? VolunteerRosterShift { get; set; }


        // ============================================================
        // ATTENDANCE DATE
        // ============================================================

        [Required]
        public DateTime AttendanceDate { get; set; }


        // ============================================================
        // ACTUAL ATTENDANCE
        // ============================================================

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }


        // ============================================================
        // ATTENDANCE STATUS
        // ============================================================

        [Required]
        public VolunteerAttendanceStatus Status { get; set; }
            = VolunteerAttendanceStatus.Scheduled;


        // ============================================================
        // ATTENDANCE CALCULATIONS
        // ============================================================

        public int LateMinutes { get; set; }

        public int EarlyDepartureMinutes { get; set; }


        // ============================================================
        // BIOMETRIC VERIFICATION
        // ============================================================

        public bool FaceVerified { get; set; }

        public bool LivenessVerified { get; set; }

        [StringLength(50)]
        public string? VerificationProvider { get; set; }

        [StringLength(500)]
        public string? VerificationReference { get; set; }


        // ============================================================
        // NOTES
        // ============================================================

        [StringLength(2000)]
        public string? Notes { get; set; }


        // ============================================================
        // AUDIT
        // ============================================================

        [Required]
        public DateTime CreatedDateTime { get; set; }
            = DateTime.Now;

        public DateTime? ModifiedDateTime { get; set; }

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        [StringLength(450)]
        public string? ModifiedBy { get; set; }
    }


    // ================================================================
    // ATTENDANCE STATUS
    // ================================================================

    public enum VolunteerAttendanceStatus
    {
        [Display(Name = "Scheduled")]
        Scheduled = 1,

        [Display(Name = "Present")]
        Present = 2,

        [Display(Name = "Late")]
        Late = 3,

        [Display(Name = "Absent")]
        Absent = 4,

        [Display(Name = "Checked Out")]
        CheckedOut = 5,

        [Display(Name = "Unscheduled")]
        Unscheduled = 6,

        [Display(Name = "Excused")]
        Excused = 7,

        [Display(Name = "Cancelled")]
        Cancelled = 8
    }
}