using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using swiftcare_api.Models.VolunteerOnboarding;

namespace swiftcare_api.Models
{
    public class Volunteer : UserBaseModel
    {
        // ============================================================
        // PROFILE
        // ============================================================

        public string? Bio { get; set; }

        public string? Motivation { get; set; }

        public VolunteerArea? PreferredArea { get; set; }


        // ============================================================
        // EMERGENCY CONTACT
        // ============================================================

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }

        public string? EmergencyContactRelationship { get; set; }


        // ============================================================
        // VOLUNTEER IDENTIFICATION
        // ============================================================

        /// <summary>
        /// Official SwiftCare volunteer number assigned when
        /// onboarding is successfully completed.
        /// Example: VOL-260915-0001
        /// </summary>
        [StringLength(30)]
        public string? VolunteerNumber { get; set; }


        /// <summary>
        /// The application from which this volunteer was created.
        /// </summary>
        public int? VolunteerApplicationId { get; set; }


        /// <summary>
        /// The onboarding process that resulted in this volunteer
        /// profile being created.
        /// </summary>
        public int? VolunteerOnboardingId { get; set; }


        // ============================================================
        // VOLUNTEER STATUS
        // ============================================================

        /// <summary>
        /// A volunteer should normally only become Active after
        /// onboarding has been fully verified.
        /// </summary>
        [Required]
        public VolunteerStatus Status { get; set; }
            = VolunteerStatus.PendingActivation;


        /// <summary>
        /// Date on which the volunteer officially became active.
        /// </summary>
        public DateTime? ActivatedDate { get; set; }


        /// <summary>
        /// Date on which onboarding was fully completed.
        /// </summary>
        public DateTime? OnboardingCompletedDate { get; set; }


        /// <summary>
        /// Optional volunteer start date.
        /// </summary>
        public DateTime? StartDate { get; set; }


        /// <summary>
        /// Optional volunteer end date.
        /// </summary>
        public DateTime? EndDate { get; set; }


        // ============================================================
        // ORGANISATIONAL INFORMATION
        // ============================================================

        [StringLength(150)]
        public string? Department { get; set; }


        /// <summary>
        /// Identity ID of the volunteer's supervisor.
        /// </summary>
        [StringLength(450)]
        public string? SupervisorId { get; set; }


        /// <summary>
        /// The supervisor responsible for this volunteer.
        /// </summary>
        [ForeignKey(nameof(SupervisorId))]
        public Supervisor? Supervisor { get; set; }


        // ============================================================
        // EXISTING VOLUNTEER DATA
        // ============================================================

        public ICollection<VolunteerExperience> Experiences { get; set; }
            = new List<VolunteerExperience>();

        public ICollection<VolunteerSkill> Skills { get; set; }
            = new List<VolunteerSkill>();

        public ICollection<VolunteerAvailability> Availability { get; set; }
            = new List<VolunteerAvailability>();

        public ICollection<VolunteerApplication> Applications { get; set; }
            = new List<VolunteerApplication>();

        public ICollection<VolunteerTaskAssignment> TaskAssignments { get; set; }
            = new List<VolunteerTaskAssignment>();


        // ============================================================
        // WEEKLY ROSTERS
        // ============================================================

        /// <summary>
        /// Weekly rosters created for this volunteer.
        /// A volunteer may have multiple rosters over time,
        /// but each roster represents one specific week.
        /// </summary>
        public ICollection<VolunteerRoster> Rosters { get; set; }
            = new List<VolunteerRoster>();


        // ============================================================
        // ONBOARDING
        // ============================================================

        /// <summary>
        /// The onboarding record that created this volunteer.
        /// </summary>
        public virtual swiftcare_api.Models.VolunteerOnboarding.VolunteerOnboarding? Onboarding { get; set; }
    }


    // ================================================================
    // VOLUNTEER STATUS
    // ================================================================

    public enum VolunteerStatus
    {
        [Display(Name = "Pending Activation")]
        PendingActivation = 1,

        [Display(Name = "Active")]
        Active = 2,

        [Display(Name = "Inactive")]
        Inactive = 3,

        [Display(Name = "Suspended")]
        Suspended = 4,

        [Display(Name = "Completed")]
        Completed = 5,

        [Display(Name = "Terminated")]
        Terminated = 6
    }
}