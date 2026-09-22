using System;
using System.Threading;
using System.Threading.Tasks;

namespace swiftcare_api.Services.IdentityVerification
{
    public interface IDiditService
    {
        Task<DiditVerificationSessionResult>
            CreateVerificationSessionAsync(
                DiditVerificationSessionRequest request,
                CancellationToken cancellationToken = default);

        Task<DiditVerificationResult?>
            GetVerificationResultAsync(
                string providerVerificationId,
                CancellationToken cancellationToken = default);

        Task<bool>
            ResendVerificationEmailAsync(
                string email,
                string applicantName,
                string verificationUrl,
                string verificationReference,
                CancellationToken cancellationToken = default);
    }

    public class DiditVerificationSessionRequest
    {
        public string ReferenceId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? CountryCode { get; set; }
    }

    public class DiditVerificationSessionResult
    {
        public bool Success { get; set; }

        public string? SessionId { get; set; }

        public string? VerificationId { get; set; }

        public string? VerificationUrl { get; set; }

        public string? Status { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public class DiditVerificationResult
    {
        public string? VerificationId { get; set; }

        public string? SessionId { get; set; }

        public string? PersonId { get; set; }

        public bool DocumentVerified { get; set; }

        public bool LivenessVerified { get; set; }

        public bool FaceMatchVerified { get; set; }

        public string? Status { get; set; }

        public string? FailureReason { get; set; }
    }
}