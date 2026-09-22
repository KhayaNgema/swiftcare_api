using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using swiftcare_api.Data;
using swiftcare_api.Models.VolunteerOnboarding;
using swiftcare_api.Services.IdentityVerification;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace swiftcare_api.Services.IdentityVerification
{
    public class VolunteerIdentityVerificationService
        : IVolunteerIdentityVerificationService
    {
        private readonly swiftcare_apiDbContext _context;
        private readonly IDiditService _diditService;
        private readonly ILogger<VolunteerIdentityVerificationService> _logger;

        public VolunteerIdentityVerificationService(
            swiftcare_apiDbContext context,
            IDiditService diditService,
            ILogger<VolunteerIdentityVerificationService> logger)
        {
            _context = context;
            _diditService = diditService;
            _logger = logger;
        }

        // =============================================================
        // START IDENTITY VERIFICATION
        // =============================================================

        public async Task<VolunteerIdentityVerification>
            StartIdentityVerificationAsync(
                int onboardingId,
                int identityDocumentId,
                CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        // =============================================================
        // GET CURRENT VERIFICATION
        // =============================================================

        public async Task<VolunteerIdentityVerification?>
            GetVerificationAsync(
                int onboardingId,
                CancellationToken cancellationToken = default)
        {
            return await _context
                .VolunteerIdentityVerifications

                .Include(x =>
                    x.IdentityDocument)

                .Include(x =>
                    x.VolunteerOnboarding)

                .Include(x =>
                    x.Volunteer)

                .Where(x =>
                    x.VolunteerOnboardingId ==
                    onboardingId)

                .OrderByDescending(x =>
                    x.CreatedDate)

                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        // =============================================================
        // PROCESS DIDIT RESULT
        // =============================================================

        public async Task ProcessDiditResultAsync(
            int verificationId,
            DiditVerificationResult result,
            CancellationToken cancellationToken = default)
        {
            if (result == null)
            {
                throw new ArgumentNullException(
                    nameof(result));
            }

            // ---------------------------------------------------------
            // LOAD VERIFICATION
            // ---------------------------------------------------------

            var verification =
                await _context
                    .VolunteerIdentityVerifications
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == verificationId,
                        cancellationToken);

            if (verification == null)
            {
                throw new InvalidOperationException(
                    $"Identity verification {verificationId} was not found.");
            }

            // =========================================================
            // STORE DIDIT SESSION ID
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    result.SessionId))
            {
                verification.ProviderSessionId =
                    result.SessionId;
            }

            // =========================================================
            // STORE PROVIDER VERIFICATION ID
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    result.VerificationId))
            {
                verification.ProviderVerificationId =
                    result.VerificationId;
            }

            // =========================================================
            // STORE PROVIDER PERSON ID
            // =========================================================
            //
            // IMPORTANT:
            //
            // Only store a real persistent Didit person identity.
            //
            // Never manufacture one locally.
            //
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    result.PersonId))
            {
                verification.ProviderPersonId =
                    result.PersonId;
            }

            // =========================================================
            // STORE VERIFICATION RESULTS
            // =========================================================

            verification.DocumentVerified =
                result.DocumentVerified;

            verification.LivenessVerified =
                result.LivenessVerified;

            verification.FaceMatchVerified =
                result.FaceMatchVerified;

            verification.FailureReason =
                result.FailureReason;

            // =========================================================
            // DETERMINE SWIFTCARE STATUS
            // =========================================================

            switch (result.Status)
            {
                // -----------------------------------------------------
                // APPROVED
                // -----------------------------------------------------

                case "Approved":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Verified;

                    verification.VerifiedDate =
                        DateTime.Now;

                    verification.FaceIdentityEnrolled =
                        verification.DocumentVerified &&
                        verification.LivenessVerified &&
                        verification.FaceMatchVerified &&
                        !string.IsNullOrWhiteSpace(
                            verification.ProviderPersonId);

                    break;

                // -----------------------------------------------------
                // DECLINED
                // -----------------------------------------------------

                case "Declined":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Rejected;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                // -----------------------------------------------------
                // IN REVIEW
                // -----------------------------------------------------

                case "In Review":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.RequiresReview;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                // -----------------------------------------------------
                // IN PROGRESS
                // -----------------------------------------------------

                case "In Progress":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.InProgress;

                    break;

                // -----------------------------------------------------
                // RESUBMITTED
                // -----------------------------------------------------

                case "Resubmitted":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.AwaitingUser;

                    verification.DocumentVerified =
                        false;

                    verification.LivenessVerified =
                        false;

                    verification.FaceMatchVerified =
                        false;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    verification.FailureReason =
                        null;

                    break;

                // -----------------------------------------------------
                // ABANDONED
                // -----------------------------------------------------

                case "Abandoned":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Cancelled;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                // -----------------------------------------------------
                // EXPIRED
                // -----------------------------------------------------

                case "Expired":

                case "KYC Expired":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Expired;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.VerifiedDate =
                        null;

                    break;

                // -----------------------------------------------------
                // UNKNOWN STATUS
                // -----------------------------------------------------

                default:

                    verification.Status =
                        VolunteerIdentityVerificationStatus.RequiresReview;

                    break;
            }

            // =========================================================
            // UPDATE AUDIT DATE
            // =========================================================

            verification.ModifiedDate =
                DateTime.Now;

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "SwiftCare identity verification {VerificationId} " +
                "updated to {Status}. " +
                "DocumentVerified={DocumentVerified}, " +
                "LivenessVerified={LivenessVerified}, " +
                "FaceMatchVerified={FaceMatchVerified}.",
                verification.Id,
                verification.Status,
                verification.DocumentVerified,
                verification.LivenessVerified,
                verification.FaceMatchVerified);
        }
    }
}
