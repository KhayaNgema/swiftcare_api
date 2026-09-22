using System.ComponentModel.DataAnnotations;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public enum VolunteerOnboardingDocumentType
    {
        [Display(Name = "Identity Document")]
        IdentityDocument = 1,

        [Display(Name = "Passport")]
        Passport = 2,

        [Display(Name = "Qualification")]
        Qualification = 3,

        [Display(Name = "Certificate")]
        Certificate = 4,

        [Display(Name = "Volunteer Contract")]
        VolunteerContract = 5,

        [Display(Name = "Proof of Address")]
        ProofOfAddress = 6,

        [Display(Name = "Professional Registration")]
        ProfessionalRegistration = 7,

        [Display(Name = "Background Check")]
        BackgroundCheck = 8,

        [Display(Name = "Medical Clearance")]
        MedicalClearance = 9,

        [Display(Name = "Other")]
        Other = 10
    }
}