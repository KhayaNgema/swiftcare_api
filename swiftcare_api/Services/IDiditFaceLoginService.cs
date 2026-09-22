using System.Threading;
using System.Threading.Tasks;

namespace swiftcare_api.Services
{
    public interface IDiditFaceLoginService
    {
        // ============================================================
        // CREATE FACE LOGIN SESSION
        // ============================================================

        Task<DiditFaceLoginSessionResult>
            CreateFaceLoginSessionAsync(
                string vendorData,
                CancellationToken cancellationToken = default);

        // ============================================================
        // GET FACE LOGIN DECISION
        // ============================================================

        Task<DiditFaceLoginDecisionResult?>
            GetFaceLoginDecisionAsync(
                string sessionId,
                CancellationToken cancellationToken = default);
    }

    // ================================================================
    // DIDIT FACE LOGIN SESSION RESULT
    // ================================================================

    public class DiditFaceLoginSessionResult
    {
        public bool Success { get; set; }

        public string? SessionId { get; set; }

        public string? SessionToken { get; set; }

        public string? VerificationUrl { get; set; }

        public string? Status { get; set; }

        public string? ErrorMessage { get; set; }
    }

    // ================================================================
    // DIDIT FACE LOGIN DECISION RESULT
    // ================================================================

    public class DiditFaceLoginDecisionResult
    {
        public bool Success { get; set; }

        public string? SessionId { get; set; }

        // Current temporary face-login session vendor data.
        //
        // Example:
        //
        // SWIFTCARE-FACE-LOGIN-ABC123
        //
        // NEVER use this to identify the volunteer.
        public string? VendorData { get; set; }

        public string? Status { get; set; }

        public bool LivenessVerified { get; set; }

        public double? LivenessScore { get; set; }

        // True only when Didit returned an enrolled
        // SWIFTCARE-ID-* identity match.
        public bool FaceSearchMatched { get; set; }

        // Didit session belonging to the matched
        // original onboarding verification.
        public string? MatchedSessionId { get; set; }

        // Example:
        //
        // SWIFTCARE-ID-1012
        //
        // This is what AuthController uses to find:
        //
        // VolunteerIdentityVerification.Id = 1012
        public string? MatchedVendorData { get; set; }

        public double? MatchSimilarity { get; set; }

        public string? FailureReason { get; set; }

        public string? ErrorMessage { get; set; }
    }
}