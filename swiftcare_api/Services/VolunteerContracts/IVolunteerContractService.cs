using Microsoft.AspNetCore.Http;
using swiftcare_api.Models.VolunteerOnboarding;

namespace swiftcare_api.Services.VolunteerContracts
{
    public interface IVolunteerContractService
    {
        // ============================================================
        // GENERATION
        // ============================================================

        Task<VolunteerContract> CreateAndGenerateContractAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);

        // ============================================================
        // RETRIEVAL
        // ============================================================

        Task<VolunteerContract?> GetContractAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);

        Task<VolunteerContract?> GetContractByOnboardingAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);

        Task<VolunteerContract?> GetContractByIdAsync(
            int contractId,
            CancellationToken cancellationToken = default);

        Task<bool> ContractExistsAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);

        // ============================================================
        // APPLICANT SUBMISSION
        // ============================================================

        Task<VolunteerContract> UploadCompletedContractAsync(
            int onboardingId,
            IFormFile file,
            string applicantId,
            CancellationToken cancellationToken = default);

        Task<VolunteerContract> ResubmitContractAsync(
            int onboardingId,
            IFormFile file,
            string applicantId,
            CancellationToken cancellationToken = default);

        // ============================================================
        // ADMIN REVIEW
        // ============================================================

        Task<VolunteerContract> ApproveContractAsync(
            int onboardingId,
            string adminId,
            string? reviewNotes = null,
            CancellationToken cancellationToken = default);

        Task<VolunteerContract> RequestCorrectionAsync(
            int onboardingId,
            string adminId,
            string correctionReason,
            CancellationToken cancellationToken = default);

        Task<VolunteerContract> RejectContractAsync(
            int onboardingId,
            string adminId,
            string rejectionReason,
            CancellationToken cancellationToken = default);

        // ============================================================
        // STATUS
        // ============================================================

        Task<bool> IsContractCompletedAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);

        Task<bool> IsContractAwaitingReviewAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);

        Task<bool> IsContractCorrectionRequiredAsync(
            int onboardingId,
            CancellationToken cancellationToken = default);
    }
}