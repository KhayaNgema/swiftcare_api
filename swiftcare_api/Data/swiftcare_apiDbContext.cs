
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using swiftcare_api.Models;
using swiftcare_api.Models.VolunteerOnboarding;

namespace swiftcare_api.Data;

public class swiftcare_apiDbContext : IdentityDbContext<UserBaseModel>
{
   

    public swiftcare_apiDbContext(DbContextOptions<swiftcare_apiDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserBaseModel>()
            .HasDiscriminator<string>("Discriminator")
            .HasValue<UserBaseModel>("UserBaseModel")
            .HasValue<Applicant>("Applicant")
            .HasValue<Volunteer>("Volunteer");
    }

    public DbSet<swiftcare_api.Models.Applicant> Applicants{ get; set; } = default!;

    public DbSet<swiftcare_api.Models.TransportRequest> TransportRequests { get; set; }
    public DbSet<swiftcare_api.Models.Trip> Trips { get; set; } = default!;

    public DbSet<swiftcare_api.Models.Vehicle> Vehicles { get; set; } = default!;

    public DbSet<swiftcare_api.Models.Resident> Residents { get; set; } = default!;

    public DbSet<swiftcare_api.Models.Driver> Drivers { get; set; } = default!;


    public DbSet<swiftcare_api.Models.SupplierAdmin> SupplierAdmins { get; set; } = default!;

    public DbSet<swiftcare_api.Models.MaintenancePerson> MaintenancePersons { get; set; } = default!;

    public DbSet<swiftcare_api.Models.Chef> Chefs { get; set; } = default!;

    public DbSet<swiftcare_api.Models.Administrator> Administrators { get; set; } = default!;

    public DbSet<swiftcare_api.Models.NextOfKin> NextOfKins { get; set; } = default!;

    public DbSet<Supervisor> Supervisors { get; set; }
    public DbSet<VolunteerRoster> VolunteerRosters { get; set; }
    public DbSet<DiditWebhookLog> DiditWebhookLogs
    {
        get; set;
    }
    public DbSet<VolunteerIdentityVerification> VolunteerIdentityVerifications
    {
        get; set;
    }
    public DbSet<VolunteerOnboarding> VolunteerOnboardings
    {
        get;
        set;
    }

    public DbSet<swiftcare_api.Models.VolunteerOnboarding.VolunteerOffer> VolunteerOffers
    {
        get;
        set;
    }


    public DbSet<VolunteerRosterShift> VolunteerRosterShifts { get; set; }

    public DbSet<VolunteerAttendance> VolunteerAttendances { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerOnboarding.VolunteerPolicy> VolunteerPolicies
    {
        get;
        set;
    }

    public DbSet<swiftcare_api.Models.VolunteerOnboarding.VolunteerPolicyAcceptance> VolunteerPolicyAcceptances
    {
        get;
        set;
    }

    public DbSet<swiftcare_api.Models.VolunteerOnboarding.VolunteerOnboardingDocument> VolunteerOnboardingDocuments
    {
        get;
        set;
    }

    public DbSet<swiftcare_api.Models.VolunteerOnboarding.VolunteerOnboardingRequirement> VolunteerOnboardingRequirements
    {
        get;
        set;
    }
    public DbSet<VolunteerInterviewEvaluation> VolunteerInterviewEvaluations { get; set; }

    public DbSet<VolunteerPolicyItem> VolunteerPolicyItems { get; set; }

    public DbSet<VolunteerInterviewSkillEvaluation> VolunteerInterviewSkillEvaluations { get; set; }

    public DbSet<VolunteerInterviewCompetencyEvaluation> VolunteerInterviewCompetencyEvaluations { get; set; }
    public DbSet<VolunteerApplicationEvaluation> VolunteerApplicationEvaluations { get; set; }

    public DbSet<VolunteerApplicationAudit> VolunteerApplicationAudits { get; set; }

    public DbSet<VolunteerVoiceInterviewSession> VolunteerVoiceInterviewSessions { get; set; }

    public DbSet<VolunteerOpportunityScoringConfiguration> VolunteerOpportunityScoringConfigurations { get; set; }
    public DbSet<swiftcare_api.Models.VolunteerQuestion> VolunteerQuestions { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerOpportunityQuestion> VolunteerOpportunityQuestions { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerApplicationAnswer> VolunteerApplicationAnswers { get; set; }
    public DbSet<swiftcare_api.Models.VolunteerRequiredSkill> VolunteerRequiredSkills { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerRequirement> VolunteerRequirements { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerOpportunity> VolunteerOpportunities { get; set; } = default!;

    public DbSet<swiftcare_api.Models.Volunteer> Volunteers { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerResponsibility> VolunteerResponsibilities { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerApplication> VolunteerApplications { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerExperience> VolunteerExperiences { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerSkill> VolunteerSkills { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerAvailability> VolunteerAvailabilities { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerTask> VolunteerTasks { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerTaskAssignment> VolunteerTaskAssignments { get; set; }

    public DbSet<swiftcare_api.Models.VolunteerDocument> VolunteerDocuments { get; set; }

    public DbSet<VolunteerAgreement> VolunteerAgreements { get; set; }

    public DbSet<VolunteerScreening> VolunteerScreenings { get; set; }

    public DbSet<swiftcare_api.Models.Doctor> Doctors { get; set; } = default!;
    public DbSet<swiftcare_api.Models.Nurse> Nurses { get; set; } = default!;

    public DbSet<VolunteerContract> VolunteerContracts { get; set; }
}
