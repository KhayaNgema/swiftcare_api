
using Microsoft.EntityFrameworkCore;
using swiftcare_api.Data;
using swiftcare_api.Models.VolunteerOnboarding;
using swiftcare_api.Data;
using swiftcare_api.Services.VolunteerContracts;
using System.Diagnostics.Contracts;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace swiftcare_api.Services.IdentityVerification
{
    public class DiditWebhookService : IDiditWebhookService
    {
        private readonly swiftcare_apiDbContext _context;
        private readonly ILogger<DiditWebhookService> _logger;
        private readonly IVolunteerContractService _volunteerContractService;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public DiditWebhookService(
             swiftcare_apiDbContext context,
            ILogger<DiditWebhookService> logger,
            IVolunteerContractService volunteerContractService)
        {
            _context = context;
            _logger = logger;
            _volunteerContractService = volunteerContractService;
        }

// ================================================================
// PROCESS DIDIT WEBHOOK
// ================================================================

public async Task ProcessWebhookAsync(
    int webhookLogId,
    CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "========== DIDIT WEBHOOK PROCESSING STARTED ==========");

            _logger.LogInformation(
                "Processing Didit webhook log {WebhookLogId}.",
                webhookLogId);

            var webhook =
                await _context.DiditWebhookLogs
                    .FirstOrDefaultAsync(
                        x => x.Id == webhookLogId,
                        cancellationToken);

            if (webhook == null)
            {
                _logger.LogWarning(
                    "Didit webhook log {WebhookLogId} was not found.",
                    webhookLogId);

                return;
            }

            if (webhook.Processed)
            {
                _logger.LogInformation(
                    "Didit webhook log {WebhookLogId} was already processed.",
                    webhookLogId);

                return;
            }

            try
            {
                // ============================================================
                // RAW BODY
                // ============================================================

                if (string.IsNullOrWhiteSpace(
                        webhook.RawBody))
                {
                    throw new InvalidOperationException(
                        "Didit webhook RawBody is empty.");
                }

                _logger.LogInformation(
                    "Raw Didit webhook body: {RawBody}",
                    webhook.RawBody);


                // ============================================================
                // DESERIALIZE
                // ============================================================

                var envelope =
                    JsonSerializer.Deserialize<DiditWebhookEnvelope>(
                        webhook.RawBody,
                        JsonOptions);

                if (envelope == null)
                {
                    throw new InvalidOperationException(
                        "Unable to deserialize Didit webhook.");
                }

                _logger.LogInformation(
                    "Didit webhook parsed successfully. " +
                    "EventId={EventId}, " +
                    "Type={WebhookType}, " +
                    "SessionId={SessionId}, " +
                    "VendorData={VendorData}, " +
                    "Status={Status}.",
                    envelope.EventId,
                    envelope.WebhookType,
                    envelope.SessionId,
                    envelope.VendorData,
                    envelope.Status);


                // ============================================================
                // PROCESS EVENT
                // ============================================================

                switch (envelope.WebhookType)
                {
                    case "status.updated":

                    case "data.updated":

                        await ProcessSessionEventAsync(
                            envelope,
                            cancellationToken);

                        break;


                    case "user.status.updated":

                        await ProcessUserStatusEventAsync(
                            envelope,
                            cancellationToken);

                        break;


                    case "user.data.updated":

                        await ProcessUserDataEventAsync(
                            envelope,
                            cancellationToken);

                        break;


                    case "business.status.updated":

                    case "business.data.updated":

                        await ProcessBusinessEventAsync(
                            envelope,
                            cancellationToken);

                        break;


                    case "activity.created":

                        await ProcessActivityEventAsync(
                            envelope,
                            cancellationToken);

                        break;


                    case "transaction.created":

                    case "transaction.status.updated":

                        await ProcessTransactionEventAsync(
                            envelope,
                            cancellationToken);

                        break;


                    default:

                        _logger.LogWarning(
                            "Unsupported Didit webhook type {WebhookType}. " +
                            "EventId={EventId}.",
                            envelope.WebhookType,
                            envelope.EventId);

                        break;
                }


                // ============================================================
                // MARK WEBHOOK AS PROCESSED
                // ============================================================

                webhook.Processed =
                    true;

                webhook.ProcessingFailed =
                    false;

                webhook.ProcessedDate =
                    DateTime.Now;

                webhook.ProcessingError =
                    null;

                await _context.SaveChangesAsync(
                    cancellationToken);


                _logger.LogInformation(
                    "Didit webhook log {WebhookLogId} processed successfully.",
                    webhookLogId);

                _logger.LogInformation(
                    "========== DIDIT WEBHOOK PROCESSING FINISHED ==========");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Didit webhook processing cancelled. " +
                    "WebhookLogId={WebhookLogId}.",
                    webhookLogId);

                throw;
            }
            catch (Exception ex)
            {
                webhook.ProcessingFailed =
                    true;

                webhook.Processed =
                    false;

                webhook.ProcessingError =
                    ex.Message.Length > 4000
                        ? ex.Message[..4000]
                        : ex.Message;

                await _context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "FAILED processing Didit webhook {WebhookLogId}.",
                    webhookLogId);

                throw;
            }
        }


        // ================================================================
        // PROCESS SESSION EVENT
        // ================================================================

        private async Task ProcessSessionEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing Didit session event. " +
                "EventId={EventId}, " +
                "Status={Status}, " +
                "VendorData={VendorData}.",
                envelope.EventId,
                envelope.Status,
                envelope.VendorData);


            // ============================================================
            // VENDOR DATA
            // ============================================================

            if (string.IsNullOrWhiteSpace(
                    envelope.VendorData))
            {
                _logger.LogWarning(
                    "Didit session webhook {EventId} has no vendor_data.",
                    envelope.EventId);

                return;
            }


            // ============================================================
            // RESOLVE VERIFICATION ID
            // ============================================================

            var verificationId =
                ExtractVerificationId(
                    envelope.VendorData);

            if (!verificationId.HasValue)
            {
                _logger.LogWarning(
                    "Unable to resolve SwiftCare verification from vendor_data {VendorData}.",
                    envelope.VendorData);

                return;
            }

            _logger.LogInformation(
                "Resolved Didit vendor_data {VendorData} to SwiftCare " +
                "verification ID {VerificationId}.",
                envelope.VendorData,
                verificationId.Value);


            // ============================================================
            // LOAD VERIFICATION
            // ============================================================

            var verification =
                await _context
                    .VolunteerIdentityVerifications
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            verificationId.Value,
                        cancellationToken);

            if (verification == null)
            {
                _logger.LogWarning(
                    "VolunteerIdentityVerification {VerificationId} was not found.",
                    verificationId.Value);

                return;
            }

            _logger.LogInformation(
                "Found VolunteerIdentityVerification {VerificationId}. " +
                "Current status={CurrentStatus}, " +
                "ProviderSessionId={ProviderSessionId}.",
                verification.Id,
                verification.Status,
                verification.ProviderSessionId);


            // ============================================================
            // IGNORE LATE IN-PROGRESS EVENT
            // ============================================================

            if (
                verification.Status ==
                VolunteerIdentityVerificationStatus.Verified &&
                string.Equals(
                    envelope.Status,
                    "In Progress",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Ignoring late In Progress event for already verified " +
                    "verification {VerificationId}.",
                    verification.Id);

                return;
            }


            // ============================================================
            // SESSION ID
            // ============================================================

            if (string.IsNullOrWhiteSpace(
                    verification.ProviderSessionId))
            {
                verification.ProviderSessionId =
                    envelope.SessionId;
            }


            // ============================================================
            // HANDLE DIDIT STATUS
            // ============================================================

            switch (envelope.Status)
            {
                // ========================================================
                // APPROVED
                // ========================================================

                case "Approved":

                    _logger.LogInformation(
                        "Didit verification {VerificationId} received APPROVED status.",
                        verification.Id);


                    verification.Status =
                        VolunteerIdentityVerificationStatus.Verified;

                    verification.DocumentVerified =
                        true;

                    verification.VerifiedDate =
                        DateTime.UtcNow;

                    verification.FailureReason =
                        null;

                    verification.ModifiedDate =
                        DateTime.Now;


                    ApplyDecisionData(
                        verification,
                        envelope.Decision);


                    // ----------------------------------------------------
                    // SAVE VERIFIED DIDIT STATE FIRST
                    // ----------------------------------------------------

                    await _context.SaveChangesAsync(
                        cancellationToken);


                    _logger.LogInformation(
                        "Didit verification {VerificationId} saved as VERIFIED. " +
                        "Document={DocumentVerified}, " +
                        "Liveness={LivenessVerified}, " +
                        "FaceMatch={FaceMatchVerified}.",
                        verification.Id,
                        verification.DocumentVerified,
                        verification.LivenessVerified,
                        verification.FaceMatchVerified);


                    // ----------------------------------------------------
                    // GENERATE / REUSE CONTRACT
                    //
                    // IMPORTANT:
                    // Do NOT perform a duplicate contract check here.
                    //
                    // TryGenerateVolunteerContractAsync delegates that
                    // responsibility to CreateAndGenerateContractAsync().
                    // ----------------------------------------------------

                    await TryGenerateVolunteerContractAsync(
                        verification,
                        cancellationToken);

                    break;


                // ========================================================
                // DECLINED
                // ========================================================

                case "Declined":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Rejected;

                    verification.DocumentVerified =
                        false;

                    verification.FaceMatchVerified =
                        false;

                    verification.LivenessVerified =
                        false;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.FailureReason =
                        ExtractDecisionWarning(
                            envelope.Decision);

                    verification.ModifiedDate =
                        DateTime.Now;

                    _logger.LogWarning(
                        "Didit verification {VerificationId} marked REJECTED. " +
                        "Reason={Reason}.",
                        verification.Id,
                        verification.FailureReason);

                    break;


                // ========================================================
                // IN REVIEW
                // ========================================================

                case "In Review":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.RequiresReview;

                    verification.ModifiedDate =
                        DateTime.Now;

                    ApplyDecisionData(
                        verification,
                        envelope.Decision);

                    _logger.LogInformation(
                        "Didit verification {VerificationId} marked REQUIRES REVIEW.",
                        verification.Id);

                    break;


                // ========================================================
                // IN PROGRESS
                // ========================================================

                case "In Progress":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.InProgress;

                    verification.ModifiedDate =
                        DateTime.Now;

                    _logger.LogInformation(
                        "Didit verification {VerificationId} marked IN PROGRESS.",
                        verification.Id);

                    break;


                // ========================================================
                // RESUBMITTED
                // ========================================================

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

                    verification.ModifiedDate =
                        DateTime.Now;

                    break;


                // ========================================================
                // ABANDONED
                // ========================================================

                case "Abandoned":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Cancelled;

                    verification.ModifiedDate =
                        DateTime.Now;

                    break;


                // ========================================================
                // EXPIRED
                // ========================================================

                case "Expired":

                case "KYC Expired":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Expired;

                    verification.ModifiedDate =
                        DateTime.Now;

                    break;


                // ========================================================
                // UNKNOWN
                // ========================================================

                default:

                    _logger.LogWarning(
                        "Unhandled Didit session status '{Status}' " +
                        "for verification {VerificationId}.",
                        envelope.Status,
                        verification.Id);

                    verification.ModifiedDate =
                        DateTime.Now;

                    break;
            }


            // ============================================================
            // SAVE STATUS CHANGES
            //
            // For Approved this is technically already saved above,
            // but keeping this here handles all other statuses.
            // ============================================================

            await _context.SaveChangesAsync(
                cancellationToken);


            _logger.LogInformation(
                "VolunteerIdentityVerification {VerificationId} saved. " +
                "New status={Status}, " +
                "DocumentVerified={DocumentVerified}, " +
                "LivenessVerified={LivenessVerified}, " +
                "FaceMatchVerified={FaceMatchVerified}.",
                verification.Id,
                verification.Status,
                verification.DocumentVerified,
                verification.LivenessVerified,
                verification.FaceMatchVerified);
        }


        // ================================================================
        // TRY GENERATE VOLUNTEER CONTRACT
        // ================================================================

        private async Task TryGenerateVolunteerContractAsync(
            VolunteerIdentityVerification verification,
            CancellationToken cancellationToken)
        {
            // ============================================================
            // DIDIT MUST BE VERIFIED
            // ============================================================

            if (verification.Status !=
                VolunteerIdentityVerificationStatus.Verified)
            {
                _logger.LogInformation(
                    "CONTRACT GENERATION SKIPPED: Identity verification " +
                    "{VerificationId} is not Verified. Status={Status}.",
                    verification.Id,
                    verification.Status);

                return;
            }


            // ============================================================
            // LOAD ONBOARDING
            // ============================================================

            var onboarding =
                await _context.VolunteerOnboardings
                    .FirstOrDefaultAsync(
                        x =>
                            x.VolunteerOnboardingId ==
                            verification.VolunteerOnboardingId,
                        cancellationToken);

            if (onboarding == null)
            {
                _logger.LogWarning(
                    "CONTRACT GENERATION SKIPPED: " +
                    "Onboarding {OnboardingId} not found.",
                    verification.VolunteerOnboardingId);

                return;
            }


            // ============================================================
            // IMPORTANT
            //
            // DO NOT CHECK VolunteerContracts HERE.
            //
            // CreateAndGenerateContractAsync() already handles:
            //
            // - no existing contract
            //      -> creates and generates it
            //
            // - existing generated contract
            //      -> reuses it
            //
            // - existing contract with missing GeneratedFilePath
            //      -> regenerates the PDF
            //
            // This method should simply delegate to that service.
            // ============================================================

            _logger.LogInformation(
                "CONTRACT GENERATION STARTING: " +
                "Didit verification succeeded. " +
                "OnboardingId={OnboardingId}, " +
                "VerificationId={VerificationId}.",
                onboarding.VolunteerOnboardingId,
                verification.Id);


            try
            {
                var contract =
                    await _volunteerContractService
                        .CreateAndGenerateContractAsync(
                            onboarding.VolunteerOnboardingId,
                            cancellationToken);


                // ========================================================
                // VERIFY RESULT
                // ========================================================

                if (contract == null)
                {
                    _logger.LogError(
                        "CONTRACT GENERATION FAILED: " +
                        "CreateAndGenerateContractAsync returned null. " +
                        "OnboardingId={OnboardingId}.",
                        onboarding.VolunteerOnboardingId);

                    return;
                }


                _logger.LogInformation(
                    "CONTRACT GENERATION COMPLETED: " +
                    "OnboardingId={OnboardingId}, " +
                    "ContractId={ContractId}, " +
                    "ContractReference={ContractReference}, " +
                    "GeneratedFileName={GeneratedFileName}, " +
                    "GeneratedFilePath={GeneratedFilePath}, " +
                    "Status={Status}.",
                    onboarding.VolunteerOnboardingId,
                    contract.VolunteerContractId,
                    contract.ContractReference,
                    contract.GeneratedFileName,
                    contract.GeneratedFilePath,
                    contract.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "CONTRACT GENERATION FAILED: " +
                    "OnboardingId={OnboardingId}, " +
                    "VerificationId={VerificationId}.",
                    onboarding.VolunteerOnboardingId,
                    verification.Id);

                throw;
            }
        }

        private static void ApplyDecisionData(
            VolunteerIdentityVerification verification,
            JsonElement? decision)
        {
            if (!decision.HasValue ||
                decision.Value.ValueKind !=
                JsonValueKind.Object)
            {
                return;
            }

            var decisionValue =
                decision.Value;

            if (
                decisionValue.TryGetProperty(
                    "id_verifications",
                    out var idVerifications) &&
                idVerifications.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (
                    var idVerification
                    in idVerifications.EnumerateArray())
                {
                    if (
                        idVerification.TryGetProperty(
                            "status",
                            out var idStatus) &&
                        string.Equals(
                            idStatus.GetString(),
                            "Approved",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        verification.DocumentVerified =
                            true;
                    }
                }
            }

            if (
                decisionValue.TryGetProperty(
                    "liveness_checks",
                    out var livenessChecks) &&
                livenessChecks.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (
                    var liveness
                    in livenessChecks.EnumerateArray())
                {
                    if (
                        liveness.TryGetProperty(
                            "status",
                            out var statusProperty) &&
                        string.Equals(
                            statusProperty.GetString(),
                            "Approved",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        verification.LivenessVerified =
                            true;
                    }
                }
            }

            if (
                decisionValue.TryGetProperty(
                    "face_matches",
                    out var faceMatches) &&
                faceMatches.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (
                    var faceMatch
                    in faceMatches.EnumerateArray())
                {
                    if (
                        faceMatch.TryGetProperty(
                            "status",
                            out var statusProperty) &&
                        string.Equals(
                            statusProperty.GetString(),
                            "Approved",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        verification.FaceMatchVerified =
                            true;
                    }
                }
            }

            if (
                verification.DocumentVerified &&
                verification.LivenessVerified &&
                verification.FaceMatchVerified &&
                !string.IsNullOrWhiteSpace(
                    verification.ProviderPersonId))
            {
                verification.FaceIdentityEnrolled =
                    true;
            }
        }

        private static string? ExtractDecisionWarning(
            JsonElement? decision)
        {
            if (!decision.HasValue ||
                decision.Value.ValueKind !=
                JsonValueKind.Object)
            {
                return null;
            }

            if (
                !decision.Value.TryGetProperty(
                    "warnings",
                    out var warnings))
            {
                return null;
            }

            if (
                warnings.ValueKind !=
                JsonValueKind.Array)
            {
                return null;
            }

            var values =
                warnings.EnumerateArray()
                    .Select(x =>
                        x.ValueKind ==
                        JsonValueKind.String
                            ? x.GetString()
                            : x.ToString())
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .ToList();

            return values.Count == 0
                ? null
                : string.Join(
                    "; ",
                    values);
        }

        private Task ProcessUserStatusEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit user status event {EventId}: {Status}.",
                envelope.EventId,
                envelope.Status);

            return Task.CompletedTask;
        }

        private Task ProcessUserDataEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit user data event {EventId}.",
                envelope.EventId);

            return Task.CompletedTask;
        }

        private Task ProcessBusinessEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit business event {EventId}: {WebhookType}.",
                envelope.EventId,
                envelope.WebhookType);

            return Task.CompletedTask;
        }

        private Task ProcessActivityEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit activity event {EventId}.",
                envelope.EventId);

            return Task.CompletedTask;
        }

        private Task ProcessTransactionEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit transaction event {EventId}: {Status}.",
                envelope.EventId,
                envelope.Status);

            return Task.CompletedTask;
        }

        private static int? ExtractVerificationId(
            string vendorData)
        {
            const string prefix =
                "SWIFTCARE-ID-";

            if (!vendorData.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var value =
                vendorData[prefix.Length..];

            return int.TryParse(
                value,
                out var id)
                ? id
                : null;
        }
    }

    public class DiditWebhookEnvelope
    {
        [JsonPropertyName("event_id")]
        public string? EventId { get; set; }

        [JsonPropertyName("webhook_type")]
        public string? WebhookType { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("application_id")]
        public string? ApplicationId { get; set; }

        [JsonPropertyName("environment")]
        public string? Environment { get; set; }

        [JsonPropertyName("session_id")]
        public string? SessionId { get; set; }

        [JsonPropertyName("business_session_id")]
        public string? BusinessSessionId { get; set; }

        [JsonPropertyName("session_kind")]
        public string? SessionKind { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("workflow_id")]
        public string? WorkflowId { get; set; }

        [JsonPropertyName("workflow_version")]
        public int? WorkflowVersion { get; set; }

        [JsonPropertyName("vendor_data")]
        public string? VendorData { get; set; }

        [JsonPropertyName("vendor_user_id")]
        public string? VendorUserId { get; set; }

        [JsonPropertyName("vendor_business_id")]
        public string? VendorBusinessId { get; set; }

        [JsonPropertyName("metadata")]
        public JsonElement? Metadata { get; set; }

        [JsonPropertyName("decision")]
        public JsonElement? Decision { get; set; }

        [JsonPropertyName("resubmit_info")]
        public JsonElement? ResubmitInfo { get; set; }

        [JsonPropertyName("changes")]
        public JsonElement? Changes { get; set; }

        [JsonPropertyName("changed_fields")]
        public List<string>? ChangedFields { get; set; }
    }
}