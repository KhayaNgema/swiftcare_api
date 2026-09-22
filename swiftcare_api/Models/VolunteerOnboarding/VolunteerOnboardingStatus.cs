using System.ComponentModel.DataAnnotations;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public enum VolunteerOnboardingStatus
    {
        [Display(Name = "Offer Pending")]
        OfferPending = 1,

        [Display(Name = "Offer Accepted")]
        OfferAccepted = 2,

        [Display(Name = "Offer Declined")]
        OfferDeclined = 3,

        [Display(Name = "Policies Pending")]
        PoliciesPending = 4,

        [Display(Name = "Contract Pending")]
        ContractPending = 5,

        [Display(Name = "Documents Pending")]
        DocumentsPending = 6,

        [Display(Name = "Admin Verification")]
        AdminVerification = 7,

        [Display(Name = "Rejected for Correction")]
        RejectedForCorrection = 8,

        [Display(Name = "Ready for Activation")]
        ReadyForActivation = 9,

        [Display(Name = "Completed")]
        Completed = 10,

        [Display(Name = "Cancelled")]
        Cancelled = 11
    }
}