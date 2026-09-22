using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace swiftcare_api.Models
{
    public class VolunteerOpportunity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerOpportunityId { get; set; }


        [StringLength(150, MinimumLength = 3,
            ErrorMessage = "The title must be between 3 and 150 characters.")]
        public string? Title { get; set; }


        [StringLength(3000, MinimumLength = 10,
            ErrorMessage = "The description must be between 10 and 3000 characters.")]
        public string? Description { get; set; }


        public VolunteerCategory? Category { get; set; }


        public VolunteerArea? Area { get; set; }


        [Required(ErrorMessage = "Please select a city.")]
        [Display(Name = "City")]
        public string? CityName { get; set; }

        public int? GeoNameId { get; set; }

        public string? Province { get; set; }


        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }


        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }


        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }


        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }


        [Range(1, 1000,
            ErrorMessage = "The number of volunteers needed must be between 1 and 1000.")]
        public int? VolunteersNeeded { get; set; }


        [Range(0, 1000,
            ErrorMessage = "The number of volunteers accepted must be between 0 and 1000.")]
        public int VolunteersAccepted { get; set; } = 0;


        public ICollection<VolunteerRequirement> Requirements { get; set; }
            = new List<VolunteerRequirement>();


        public ICollection<VolunteerResponsibility> Responsibilities { get; set; }
            = new List<VolunteerResponsibility>();


        public ICollection<VolunteerRequiredSkill> RequiredSkills { get; set; }
            = new List<VolunteerRequiredSkill>();


        [Range(14, 100,
            ErrorMessage = "The minimum age must be between 14 and 100.")]
        public int? MinimumAge { get; set; }


        [DataType(DataType.Date)]
        public DateTime? ApplicationDeadline { get; set; }


        [StringLength(500,
            ErrorMessage = "The poster image path cannot exceed 500 characters.")]
        public string? PosterImage { get; set; }


        public VolunteerOpportunityStatus Status { get; set; }
            = VolunteerOpportunityStatus.Draft;


        public DateTime CreatedAt { get; set; }
            = DateTime.Now;


        [StringLength(450)]
        public string? CreatedById { get; set; }


        public ICollection<VolunteerOpportunityQuestion>
            ApplicationQuestions
        { get; set; }
            = new List<VolunteerOpportunityQuestion>();


        // ============================================================
        // SHORTLISTING PIPELINE
        // ============================================================

        /// <summary>
        /// Indicates whether the ATS shortlisting pipeline has
        /// successfully been executed for this opportunity.
        /// </summary>
        public bool ShortlistingPipelineRun { get; set; } = false;


        /// <summary>
        /// Date and time when the shortlisting pipeline was
        /// successfully completed.
        /// </summary>
        public DateTime? ShortlistingPipelineRunDate { get; set; }


        /// <summary>
        /// Identity/user name of the administrator who ran
        /// the shortlisting pipeline.
        /// </summary>
        [StringLength(450)]
        public string? ShortlistingPipelineRunBy { get; set; }
    }


    public enum VolunteerArea
    {
        [Display(Name = "Resident Care")]
        ResidentCare,

        [Display(Name = "Activities & Recreation")]
        ActivitiesAndRecreation,

        [Display(Name = "Dining Services")]
        DiningServices,

        [Display(Name = "Administration")]
        Administration,

        [Display(Name = "Garden & Outdoor")]
        GardenAndOutdoor,

        [Display(Name = "Events")]
        Events,

        [Display(Name = "Transportation")]
        Transportation,

        [Display(Name = "Community Outreach")]
        CommunityOutreach
    }


    public enum VolunteerOpportunityStatus
    {
        Draft,
        Published,
        Closed,
        Cancelled
    }


    public enum VolunteerCategory
    {
        [Display(Name = "Resident Support")]
        ResidentSupport,

        [Display(Name = "Activities")]
        Activities,

        [Display(Name = "Companionship")]
        Companionship,

        [Display(Name = "Events")]
        Events,

        [Display(Name = "Administration")]
        Administration,

        [Display(Name = "Recreation")]
        Recreation,

        [Display(Name = "Food Service")]
        FoodService,

        [Display(Name = "Gardening")]
        Gardening,

        [Display(Name = "Maintenance")]
        Maintenance,

        [Display(Name = "Fundraising")]
        Fundraising,

        [Display(Name = "Transportation")]
        Transportation,

        [Display(Name = "Social Support")]
        SocialSupport,

        [Display(Name = "Other")]
        Other
    }
}