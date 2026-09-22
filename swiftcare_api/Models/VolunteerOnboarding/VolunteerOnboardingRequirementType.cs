
using System.ComponentModel.DataAnnotations;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public enum VolunteerOnboardingRequirementType
    {
        [Display(Name = "Offer Acceptance")]
        OfferAcceptance = 1,

        [Display(Name = "Policy Acceptance")]
        PolicyAcceptance = 2,

        [Display(Name = "Code of Conduct")]
        CodeOfConduct = 3,

        [Display(Name = "Contract")]
        Contract = 4,

        [Display(Name = "Identity Verification")]
        IdentityVerification = 5,

        [Display(Name = "Qualification Verification")]
        QualificationVerification = 6,

        [Display(Name = "Document Upload")]
        DocumentUpload = 7,

        [Display(Name = "Supervisor Assignment")]
        SupervisorAssignment = 10,

        [Display(Name = "Other")]
        Other = 11
    }
}
