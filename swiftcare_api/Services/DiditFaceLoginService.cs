using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace swiftcare_api.Services
{
    public class DiditFaceLoginService : IDiditFaceLoginService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiditFaceLoginService> _logger;

        public DiditFaceLoginService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<DiditFaceLoginService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        // ============================================================
        // CREATE FACE LOGIN SESSION
        // ============================================================
        //
        // Creates the temporary Didit session used by Flutter.
        //
        // IMPORTANT:
        //
        // This does NOT perform a separate /v3/face-search/ request.
        //
        // The Didit workflow itself is responsible for:
        //
        //     Passive Liveness
        //            +
        //     Face Search
        //
        // if Face Search is enabled in the workflow.
        //
        // ============================================================

        public async Task<DiditFaceLoginSessionResult>
            CreateFaceLoginSessionAsync(
                string vendorData,
                CancellationToken cancellationToken = default)
        {
            var apiKey =
                _configuration["Didit:ApiKey"];

            var workflowId =
                _configuration["Didit:BiometricWorkflowId"];

            var callbackUrl =
                _configuration["Didit:CallbackUrl"];

            var baseUrl =
                _configuration["Didit:BaseUrl"]
                ?? "https://verification.didit.me/";

            baseUrl =
                baseUrl.TrimEnd('/') + "/";

            // ========================================================
            // VALIDATE API KEY
            // ========================================================

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError(
                    "Didit:ApiKey is missing.");

                return new DiditFaceLoginSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit API key is not configured."
                };
            }

            // ========================================================
            // VALIDATE WORKFLOW
            // ========================================================

            if (string.IsNullOrWhiteSpace(workflowId))
            {
                _logger.LogError(
                    "Didit:BiometricWorkflowId is missing.");

                return new DiditFaceLoginSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit BiometricWorkflowId is not configured."
                };
            }

            // ========================================================
            // VALIDATE CALLBACK
            // ========================================================

            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                _logger.LogError(
                    "Didit:CallbackUrl is missing.");

                return new DiditFaceLoginSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit CallbackUrl is not configured."
                };
            }

            // ========================================================
            // VALIDATE VENDOR DATA
            // ========================================================

            if (string.IsNullOrWhiteSpace(vendorData))
            {
                _logger.LogError(
                    "Didit vendor_data is empty.");

                return new DiditFaceLoginSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit vendor_data cannot be empty."
                };
            }

            // ========================================================
            // LOG SAFE SESSION INFORMATION
            // ========================================================

            _logger.LogInformation(
                "Preparing Didit face-login session. " +
                "BaseUrl: {BaseUrl}, " +
                "WorkflowId: {WorkflowId}, " +
                "CallbackUrl: {CallbackUrl}, " +
                "VendorData: {VendorData}",
                baseUrl,
                workflowId,
                callbackUrl,
                vendorData);

            // ========================================================
            // DIDIT PAYLOAD
            // ========================================================

            var payload = new
            {
                workflow_id = workflowId,

                vendor_data = vendorData,

                callback = callbackUrl,

                callback_method = "both",

                metadata = new
                {
                    source = "SwiftCare",
                    purpose = "face_login",
                    vendor_data = vendorData
                },

                language = "en"
            };

            var json =
                JsonSerializer.Serialize(
                    payload,
                    new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });

            // ========================================================
            // DIDIT SESSION URL
            // ========================================================

            var requestUrl =
                new Uri(
                    new Uri(baseUrl),
                    "v3/session/");

            _logger.LogInformation(
                "Sending Didit face-login session request to {Url}",
                requestUrl);

            // ========================================================
            // HTTP REQUEST
            // ========================================================

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    requestUrl);

            request.Headers.Add(
                "x-api-key",
                apiKey);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            try
            {
                using var response =
                    await _httpClient.SendAsync(
                        request,
                        cancellationToken);

                var responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "Didit face-login session response received. " +
                    "HTTP: {StatusCode}",
                    (int)response.StatusCode);

                // ====================================================
                // HANDLE HTTP ERROR
                // ====================================================

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Didit face-login session creation failed. " +
                        "HTTP {StatusCode}. Response: {Response}",
                        (int)response.StatusCode,
                        responseBody);

                    return new DiditFaceLoginSessionResult
                    {
                        Success = false,

                        ErrorMessage =
                            $"Didit session creation failed. " +
                            $"HTTP {(int)response.StatusCode}: " +
                            responseBody
                    };
                }

                // ====================================================
                // PARSE RESPONSE
                // ====================================================

                JsonNode? root;

                try
                {
                    root =
                        JsonNode.Parse(
                            responseBody);
                }
                catch (Exception jsonEx)
                {
                    _logger.LogError(
                        jsonEx,
                        "Didit returned invalid session JSON.");

                    return new DiditFaceLoginSessionResult
                    {
                        Success = false,

                        ErrorMessage =
                            "Didit returned an invalid response."
                    };
                }

                // ====================================================
                // SESSION ID
                // ====================================================

                var sessionId =
                    GetString(
                        root,
                        "session_id",
                        "sessionId",
                        "id");

                // ====================================================
                // SESSION TOKEN
                // ====================================================

                var sessionToken =
                    GetString(
                        root,
                        "session_token",
                        "sessionToken",
                        "token",
                        "session_token_value");

                // ====================================================
                // VERIFICATION URL
                // ====================================================

                var verificationUrl =
                    GetString(
                        root,
                        "url",
                        "session_url",
                        "verification_url",
                        "verificationUrl",
                        "link");

                // ====================================================
                // STATUS
                // ====================================================

                var status =
                    GetString(
                        root,
                        "status");

                _logger.LogInformation(
                    "Didit face-login session parsed. " +
                    "SessionId: {SessionId}, " +
                    "SessionTokenPresent: {SessionTokenPresent}, " +
                    "VerificationUrlPresent: {VerificationUrlPresent}, " +
                    "Status: {Status}",
                    sessionId,
                    !string.IsNullOrWhiteSpace(sessionToken),
                    !string.IsNullOrWhiteSpace(verificationUrl),
                    status);

                // ====================================================
                // SESSION ID REQUIRED
                // ====================================================

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return new DiditFaceLoginSessionResult
                    {
                        Success = false,

                        ErrorMessage =
                            "Didit did not return a session ID."
                    };
                }

                // ====================================================
                // SESSION TOKEN REQUIRED
                // ====================================================

                if (string.IsNullOrWhiteSpace(sessionToken))
                {
                    _logger.LogError(
                        "Didit created face-login session {SessionId}, " +
                        "but returned no session token.",
                        sessionId);

                    return new DiditFaceLoginSessionResult
                    {
                        Success = false,

                        SessionId =
                            sessionId,

                        VerificationUrl =
                            verificationUrl,

                        Status =
                            status,

                        ErrorMessage =
                            "Didit did not return a session token " +
                            "required by the Flutter SDK."
                    };
                }

                // ====================================================
                // SUCCESS
                // ====================================================

                return new DiditFaceLoginSessionResult
                {
                    Success = true,

                    SessionId =
                        sessionId,

                    SessionToken =
                        sessionToken,

                    VerificationUrl =
                        verificationUrl,

                    Status =
                        status
                };
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Didit face-login session request was cancelled.");

                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error communicating with Didit. " +
                    "URL: {Url}. " +
                    "Message: {Message}",
                    requestUrl,
                    ex.Message);

                return new DiditFaceLoginSessionResult
                {
                    Success = false,

                    ErrorMessage =
                        $"Could not connect to Didit: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error creating Didit face-login session. " +
                    "Message: {Message}",
                    ex.Message);

                return new DiditFaceLoginSessionResult
                {
                    Success = false,

                    ErrorMessage =
                        $"Unexpected error creating Didit session: {ex.Message}"
                };
            }
        }

        // ============================================================
        // GET FACE LOGIN DECISION
        // ============================================================
        //
        // IMPORTANT:
        //
        // This only retrieves the decision from the existing Didit
        // login session.
        //
        // It does NOT call:
        //
        //     /v3/face-search/
        //
        // Any face matching must already have been performed by the
        // configured Didit workflow.
        //
        // ============================================================

        public async Task<DiditFaceLoginDecisionResult?>
            GetFaceLoginDecisionAsync(
                string sessionId,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return null;
            }

            sessionId =
                sessionId.Trim();

            var apiKey =
                _configuration["Didit:ApiKey"];

            var baseUrl =
                _configuration["Didit:BaseUrl"]
                ?? "https://verification.didit.me/";

            baseUrl =
                baseUrl.TrimEnd('/') + "/";

            // ========================================================
            // API KEY
            // ========================================================

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError(
                    "Didit:ApiKey is missing while retrieving " +
                    "face-login decision.");

                return new DiditFaceLoginDecisionResult
                {
                    Success = false,

                    SessionId =
                        sessionId,

                    ErrorMessage =
                        "Didit API key is not configured."
                };
            }

            // ========================================================
            // DECISION URL
            // ========================================================

            var requestUrl =
                new Uri(
                    new Uri(baseUrl),
                    $"v3/session/{Uri.EscapeDataString(sessionId)}/decision/");

            _logger.LogInformation(
                "Requesting Didit face-login decision. " +
                "SessionId: {SessionId}",
                sessionId);

            // ========================================================
            // HTTP REQUEST
            // ========================================================

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUrl);

            request.Headers.Add(
                "x-api-key",
                apiKey);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            try
            {
                using var response =
                    await _httpClient.SendAsync(
                        request,
                        cancellationToken);

                var responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "Didit face-login decision response received. " +
                    "SessionId: {SessionId}, " +
                    "HTTP: {StatusCode}",
                    sessionId,
                    (int)response.StatusCode);

                // ====================================================
                // HTTP ERROR
                // ====================================================

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Didit decision lookup failed. " +
                        "SessionId: {SessionId}, " +
                        "HTTP: {StatusCode}. " +
                        "Response: {Response}",
                        sessionId,
                        (int)response.StatusCode,
                        responseBody);

                    return new DiditFaceLoginDecisionResult
                    {
                        Success = false,

                        SessionId =
                            sessionId,

                        ErrorMessage =
                            $"Didit decision lookup failed. " +
                            $"HTTP {(int)response.StatusCode}: " +
                            responseBody
                    };
                }

                // ====================================================
                // PARSE JSON
                // ====================================================

                JsonNode? root;

                try
                {
                    root =
                        JsonNode.Parse(
                            responseBody);
                }
                catch (Exception jsonEx)
                {
                    _logger.LogError(
                        jsonEx,
                        "Could not parse Didit decision JSON. " +
                        "SessionId: {SessionId}",
                        sessionId);

                    return new DiditFaceLoginDecisionResult
                    {
                        Success = false,

                        SessionId =
                            sessionId,

                        ErrorMessage =
                            "Didit returned an invalid decision response."
                    };
                }

                return ParseDecision(
                    root,
                    sessionId);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error retrieving Didit decision. " +
                    "SessionId: {SessionId}. " +
                    "Message: {Message}",
                    sessionId,
                    ex.Message);

                return new DiditFaceLoginDecisionResult
                {
                    Success = false,

                    SessionId =
                        sessionId,

                    ErrorMessage =
                        $"Could not connect to Didit: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error retrieving Didit decision. " +
                    "SessionId: {SessionId}",
                    sessionId);

                return new DiditFaceLoginDecisionResult
                {
                    Success = false,

                    SessionId =
                        sessionId,

                    ErrorMessage =
                        $"Unexpected error retrieving Didit decision: {ex.Message}"
                };
            }
        }

        // ============================================================
        // PARSE DIDIT DECISION
        // ============================================================

        private DiditFaceLoginDecisionResult ParseDecision(
            JsonNode? root,
            string sessionId)
        {
            var result =
                new DiditFaceLoginDecisionResult
                {
                    Success = true,

                    SessionId =
                        sessionId
                };

            if (root == null)
            {
                result.Success = false;

                result.ErrorMessage =
                    "Didit returned an empty decision response.";

                return result;
            }

            // ========================================================
            // TOP LEVEL STATUS
            // ========================================================

            result.Status =
                GetString(
                    root,
                    "status");

            // ========================================================
            // CURRENT LOGIN VENDOR DATA
            // ========================================================
            //
            // Example:
            //
            // SWIFTCARE-FACE-LOGIN-831973...
            //
            // THIS IS NEVER USED AS THE USER IDENTITY.
            //
            // ========================================================

            result.VendorData =
                GetString(
                    root,
                    "vendor_data",
                    "vendorData");

            // ========================================================
            // FAILURE REASON
            // ========================================================

            result.FailureReason =
                GetString(
                    root,
                    "failure_reason",
                    "failureReason",
                    "reason");

            // ========================================================
            // LIVENESS CHECKS
            // ========================================================

            var livenessChecks =
                FindDirectArray(
                    root,
                    "liveness_checks");

            var totalMatchCount =
                0;

            var enrolledMatchCount =
                0;

            var candidates =
                new List<FaceMatchCandidate>();

            // ========================================================
            // PROCESS LIVENESS CHECKS
            // ========================================================

            if (livenessChecks != null)
            {
                foreach (var livenessCheck in livenessChecks)
                {
                    if (livenessCheck == null)
                    {
                        continue;
                    }

                    // =================================================
                    // LIVENESS METHOD
                    // =================================================

                    var method =
                        GetString(
                            livenessCheck,
                            "method");

                    // =================================================
                    // LIVENESS STATUS
                    // =================================================

                    var status =
                        GetString(
                            livenessCheck,
                            "status");

                    // =================================================
                    // LIVENESS SCORE
                    // =================================================

                    var score =
                        GetNullableDouble(
                            livenessCheck,
                            "score");

                    // =================================================
                    // PASSIVE LIVENESS
                    // =================================================

                    if (string.Equals(
                            method,
                            "PASSIVE",
                            StringComparison.OrdinalIgnoreCase) &&
                        IsApproved(status))
                    {
                        result.LivenessVerified =
                            true;

                        if (score.HasValue)
                        {
                            if (!result.LivenessScore.HasValue ||
                                score.Value >
                                result.LivenessScore.Value)
                            {
                                result.LivenessScore =
                                    score.Value;
                            }
                        }
                    }

                    // =================================================
                    // DIDIT AUTOMATIC FACE MATCHES
                    // =================================================
                    //
                    // IMPORTANT:
                    //
                    // We are NOT making a separate face-search call.
                    //
                    // These matches are whatever Didit returned from
                    // the configured workflow.
                    //
                    // =================================================

                    var matches =
                        FindDirectArray(
                            livenessCheck,
                            "matches");

                    if (matches == null)
                    {
                        continue;
                    }

                    foreach (var match in matches)
                    {
                        if (match == null)
                        {
                            continue;
                        }

                        totalMatchCount++;

                        // =============================================
                        // MATCH STATUS
                        // =============================================

                        var matchStatus =
                            GetString(
                                match,
                                "status");

                        // =============================================
                        // MATCHED DIDIT SESSION
                        // =============================================

                        var matchedSessionId =
                            GetString(
                                match,
                                "session_id",
                                "sessionId");

                        // =============================================
                        // DIRECT MATCH VENDOR DATA
                        // =============================================

                        var matchedVendorData =
                            GetString(
                                match,
                                "vendor_data",
                                "vendorData");

                        // =============================================
                        // EXTRACT ORIGINAL SWIFTCARE IDENTITY
                        // =============================================
                        //
                        // We deliberately do NOT simply trust
                        // matchedVendorData.
                        //
                        // We check that it is:
                        //
                        // SWIFTCARE-ID-{verificationId}
                        //
                        // instead of:
                        //
                        // SWIFTCARE-FACE-LOGIN-...
                        //
                        // =============================================

                        var enrolledVendorData =
                            ExtractSwiftCareIdentityVendorData(
                                match);

                        // =============================================
                        // SIMILARITY
                        // =============================================

                        var similarity =
                            GetNullableDouble(
                                match,
                                "similarity_percentage",
                                "similarity",
                                "similarity_score",
                                "similarityScore",
                                "score");

                        // =============================================
                        // BLOCKLIST
                        // =============================================

                        var isBlocklisted =
                            GetNullableBoolean(
                                match,
                                "is_blocklisted",
                                "isBlocklisted");

                        // =============================================
                        // ONLY APPROVED MATCHES
                        // =============================================

                        if (!IsApproved(matchStatus))
                        {
                            continue;
                        }

                        // =============================================
                        // NEVER ACCEPT BLOCKLISTED MATCHES
                        // =============================================

                        if (isBlocklisted == true)
                        {
                            _logger.LogWarning(
                                "Ignoring blocklisted Didit match. " +
                                "LoginSessionId: {LoginSessionId}, " +
                                "MatchedSessionId: {MatchedSessionId}",
                                sessionId,
                                matchedSessionId);

                            continue;
                        }

                        // =============================================
                        // SIMILARITY REQUIRED
                        // =============================================

                        if (!similarity.HasValue)
                        {
                            _logger.LogWarning(
                                "Ignoring Didit approved match " +
                                "without similarity. " +
                                "LoginSessionId: {LoginSessionId}, " +
                                "MatchedSessionId: {MatchedSessionId}",
                                sessionId,
                                matchedSessionId);

                            continue;
                        }

                        // =============================================
                        // FACE MATCH THRESHOLD
                        // =============================================

                        var threshold =
                            GetFaceMatchThreshold();

                        if (similarity.Value < threshold)
                        {
                            _logger.LogInformation(
                                "Ignoring Didit face match below " +
                                "SwiftCare threshold. " +
                                "LoginSessionId: {LoginSessionId}, " +
                                "Similarity: {Similarity}, " +
                                "Threshold: {Threshold}",
                                sessionId,
                                similarity.Value,
                                threshold);

                            continue;
                        }

                        // =============================================
                        // ORIGINAL SWIFTCARE IDENTITY REQUIRED
                        // =============================================
                        //
                        // This is the critical protection.
                        //
                        // A temporary login session such as:
                        //
                        // SWIFTCARE-FACE-LOGIN-ABC123
                        //
                        // is NEVER allowed to become the matched user.
                        //
                        // Only:
                        //
                        // SWIFTCARE-ID-123
                        //
                        // is accepted.
                        //
                        // =============================================

                        if (string.IsNullOrWhiteSpace(
                                enrolledVendorData))
                        {
                            _logger.LogWarning(
                                "Didit returned an approved face match " +
                                "but it did not contain an original " +
                                "SwiftCare identity vendor_data. " +
                                "LoginSessionId: {LoginSessionId}, " +
                                "MatchedSessionId: {MatchedSessionId}, " +
                                "MatchedVendorData: {MatchedVendorData}, " +
                                "Similarity: {Similarity}",
                                sessionId,
                                matchedSessionId,
                                matchedVendorData,
                                similarity.Value);

                            continue;
                        }

                        // =============================================
                        // VALID ENROLLED MATCH
                        // =============================================

                        enrolledMatchCount++;

                        candidates.Add(
                            new FaceMatchCandidate
                            {
                                SessionId =
                                    matchedSessionId,

                                VendorData =
                                    enrolledVendorData,

                                Similarity =
                                    similarity.Value,

                                Status =
                                    matchStatus
                            });
                    }
                }
            }

            // ========================================================
            // SELECT STRONGEST ORIGINAL SWIFTCARE IDENTITY
            // ========================================================

            var strongestMatch =
                candidates
                    .Where(
                        x =>
                            IsSwiftCareIdentityVendorData(
                                x.VendorData))
                    .OrderByDescending(
                        x =>
                            x.Similarity)
                    .FirstOrDefault();

            // ========================================================
            // ORIGINAL ONBOARDING MATCH FOUND
            // ========================================================

            if (strongestMatch != null)
            {
                result.FaceSearchMatched =
                    true;

                result.MatchedSessionId =
                    strongestMatch.SessionId;

                result.MatchedVendorData =
                    strongestMatch.VendorData;

                result.MatchSimilarity =
                    strongestMatch.Similarity;
            }
            else
            {
                // ====================================================
                // NO ORIGINAL ONBOARDING MATCH
                // ====================================================
                //
                // VERY IMPORTANT:
                //
                // NEVER fall back to:
                //
                // result.VendorData
                //
                // because that belongs to the current temporary
                // face-login attempt.
                //
                // ====================================================

                result.FaceSearchMatched =
                    false;

                result.MatchedSessionId =
                    null;

                result.MatchedVendorData =
                    null;

                result.MatchSimilarity =
                    null;
            }

            // ========================================================
            // SAFE SUMMARY LOGGING
            // ========================================================

            _logger.LogInformation(
                "Parsed Didit face-login decision. " +
                "SessionId: {SessionId}, " +
                "Status: {Status}, " +
                "LivenessVerified: {LivenessVerified}, " +
                "LivenessScore: {LivenessScore}, " +
                "FaceSearchMatched: {FaceSearchMatched}, " +
                "TotalMatchCount: {TotalMatchCount}, " +
                "EnrolledMatchCount: {EnrolledMatchCount}, " +
                "MatchedSessionId: {MatchedSessionId}, " +
                "MatchedVendorData: {MatchedVendorData}, " +
                "MatchSimilarity: {MatchSimilarity}",
                result.SessionId,
                result.Status,
                result.LivenessVerified,
                result.LivenessScore,
                result.FaceSearchMatched,
                totalMatchCount,
                enrolledMatchCount,
                result.MatchedSessionId,
                result.MatchedVendorData,
                result.MatchSimilarity);

            return result;
        }

        // ============================================================
        // EXTRACT SWIFTCARE IDENTITY VENDOR DATA
        // ============================================================
        //
        // Accepted:
        //
        //     SWIFTCARE-ID-1012
        //
        // Rejected:
        //
        //     SWIFTCARE-FACE-LOGIN-831973...
        //
        // ============================================================

        private static string?
            ExtractSwiftCareIdentityVendorData(
                JsonNode match)
        {
            // ========================================================
            // FIRST: DIRECT vendor_data
            // ========================================================

            var directVendorData =
                GetString(
                    match,
                    "vendor_data",
                    "vendorData");

            if (IsSwiftCareIdentityVendorData(
                    directVendorData))
            {
                return directVendorData;
            }

            // ========================================================
            // SECOND: SEARCH ONLY INSIDE THIS MATCH
            // ========================================================
            //
            // This allows Didit to return the original vendor data
            // nested in the match object.
            //
            // We NEVER search the entire response.
            //
            // This is important because the current login vendor data
            // also exists elsewhere in the response.
            //
            // ========================================================

            return FindSwiftCareIdentityVendorData(
                match);
        }

        // ============================================================
        // FIND ORIGINAL SWIFTCARE IDENTITY VENDOR DATA
        // ============================================================

        private static string?
            FindSwiftCareIdentityVendorData(
                JsonNode? node)
        {
            if (node == null)
            {
                return null;
            }

            // ========================================================
            // OBJECT
            // ========================================================

            if (node is JsonObject obj)
            {
                foreach (var property in obj)
                {
                    // =================================================
                    // SCALAR VALUE
                    // =================================================

                    if (property.Value is JsonValue jsonValue)
                    {
                        string? text =
                            null;

                        try
                        {
                            if (jsonValue.TryGetValue<string>(
                                    out var stringValue))
                            {
                                text =
                                    stringValue;
                            }
                        }
                        catch
                        {
                            // Ignore.
                        }

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            try
                            {
                                text =
                                    jsonValue.ToString();
                            }
                            catch
                            {
                                // Ignore.
                            }
                        }

                        if (IsSwiftCareIdentityVendorData(
                                text))
                        {
                            return text;
                        }
                    }

                    // =================================================
                    // NESTED VALUE
                    // =================================================

                    var nestedResult =
                        FindSwiftCareIdentityVendorData(
                            property.Value);

                    if (!string.IsNullOrWhiteSpace(
                            nestedResult))
                    {
                        return nestedResult;
                    }
                }

                return null;
            }

            // ========================================================
            // ARRAY
            // ========================================================

            if (node is JsonArray array)
            {
                foreach (var item in array)
                {
                    var nestedResult =
                        FindSwiftCareIdentityVendorData(
                            item);

                    if (!string.IsNullOrWhiteSpace(
                            nestedResult))
                    {
                        return nestedResult;
                    }
                }
            }

            return null;
        }

        // ============================================================
        // CHECK ORIGINAL SWIFTCARE IDENTITY VENDOR DATA
        // ============================================================

        private static bool
            IsSwiftCareIdentityVendorData(
                string? vendorData)
        {
            if (string.IsNullOrWhiteSpace(
                    vendorData))
            {
                return false;
            }

            return vendorData.StartsWith(
                "SWIFTCARE-ID-",
                StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // GET FACE MATCH THRESHOLD
        // ============================================================

        private double GetFaceMatchThreshold()
        {
            var configuredValue =
                _configuration[
                    "Didit:FaceMatchThreshold"];

            if (double.TryParse(
                    configuredValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var threshold))
            {
                if (threshold >= 0 &&
                    threshold <= 100)
                {
                    return threshold;
                }
            }

            // ========================================================
            // DEFAULT
            // ========================================================

            return 70.0;
        }

        // ============================================================
        // DIDIT APPROVED
        // ============================================================

        private static bool IsApproved(
            string? value)
        {
            return string.Equals(
                value,
                "Approved",
                StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // GET STRING
        // ============================================================

        private static string? GetString(
            JsonNode? node,
            params string[] names)
        {
            if (node == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                var value =
                    FindDirectProperty(
                        node,
                        name);

                if (value == null)
                {
                    continue;
                }

                if (value is JsonValue jsonValue)
                {
                    // =================================================
                    // STRING
                    // =================================================

                    try
                    {
                        if (jsonValue.TryGetValue<string>(
                                out var stringValue))
                        {
                            return stringValue;
                        }
                    }
                    catch
                    {
                        // Continue.
                    }

                    // =================================================
                    // FALLBACK
                    // =================================================

                    try
                    {
                        return jsonValue.ToString();
                    }
                    catch
                    {
                        // Continue.
                    }
                }
            }

            return null;
        }

        // ============================================================
        // GET DOUBLE
        // ============================================================

        private static double? GetNullableDouble(
            JsonNode? node,
            params string[] names)
        {
            if (node == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                var value =
                    FindDirectProperty(
                        node,
                        name);

                if (value is not JsonValue jsonValue)
                {
                    continue;
                }

                // ====================================================
                // DIRECT DOUBLE
                // ====================================================

                try
                {
                    if (jsonValue.TryGetValue<double>(
                            out var number))
                    {
                        return number;
                    }
                }
                catch
                {
                    // Continue.
                }

                // ====================================================
                // PARSED DOUBLE
                // ====================================================

                try
                {
                    if (double.TryParse(
                            jsonValue.ToString(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var parsed))
                    {
                        return parsed;
                    }
                }
                catch
                {
                    // Continue.
                }
            }

            return null;
        }

        // ============================================================
        // GET BOOLEAN
        // ============================================================

        private static bool? GetNullableBoolean(
            JsonNode? node,
            params string[] names)
        {
            if (node == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                var value =
                    FindDirectProperty(
                        node,
                        name);

                if (value is not JsonValue jsonValue)
                {
                    continue;
                }

                // ====================================================
                // DIRECT BOOLEAN
                // ====================================================

                try
                {
                    if (jsonValue.TryGetValue<bool>(
                            out var boolValue))
                    {
                        return boolValue;
                    }
                }
                catch
                {
                    // Continue.
                }

                // ====================================================
                // STRING BOOLEAN
                // ====================================================

                var text =
                    jsonValue.ToString();

                if (string.Equals(
                        text,
                        "true",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(
                        text,
                        "false",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return null;
        }

        // ============================================================
        // FIND DIRECT PROPERTY
        // ============================================================
        //
        // Deliberately does NOT recursively search.
        //
        // ============================================================

        private static JsonNode?
            FindDirectProperty(
                JsonNode node,
                string propertyName)
        {
            if (node is not JsonObject obj)
            {
                return null;
            }

            foreach (var property in obj)
            {
                if (string.Equals(
                        property.Key,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return null;
        }

        // ============================================================
        // FIND DIRECT ARRAY
        // ============================================================

        private static JsonArray?
            FindDirectArray(
                JsonNode? node,
                string propertyName)
        {
            if (node is not JsonObject obj)
            {
                return null;
            }

            foreach (var property in obj)
            {
                if (!string.Equals(
                        property.Key,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value is JsonArray array)
                {
                    return array;
                }

                return null;
            }

            return null;
        }

        // ============================================================
        // FACE MATCH CANDIDATE
        // ============================================================

        private sealed class FaceMatchCandidate
        {
            public string? SessionId { get; set; }

            public string? VendorData { get; set; }

            public double Similarity { get; set; }

            public string? Status { get; set; }
        }
    }
}