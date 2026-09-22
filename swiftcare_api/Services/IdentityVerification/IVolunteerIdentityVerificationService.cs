using swiftcare_api.Models.VolunteerOnboarding;

namespace swiftcare_api.Services.IdentityVerification
{
    public interface IVolunteerIdentityVerificationService
    {
        Task<VolunteerIdentityVerification>
            StartIdentityVerificationAsync(
                int onboardingId,
                int identityDocumentId,
                CancellationToken cancellationToken = default);

        Task<VolunteerIdentityVerification?>
            GetVerificationAsync(
                int onboardingId,
                CancellationToken cancellationToken = default);

        Task ProcessDiditResultAsync(
            int verificationId,
            DiditVerificationResult result,
            CancellationToken cancellationToken = default);
    }
}