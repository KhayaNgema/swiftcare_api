using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using swiftcare_api.Configuration;

namespace swiftcare_api.Services.IdentityVerification
{
    public class DiditService : IDiditService
    {
        private readonly HttpClient _httpClient;
        private readonly DiditOptions _options;
        private readonly ILogger<DiditService> _logger;
        private readonly EmailService _emailService;

        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

        public DiditService(
            HttpClient httpClient,
            IOptions<DiditOptions> options,
            ILogger<DiditService> logger,
            EmailService emailService)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
            _emailService = emailService;

            if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                _httpClient.BaseAddress =
                    new Uri(
                        _options.BaseUrl.TrimEnd('/') + "/");
            }

            _httpClient.Timeout =
                TimeSpan.FromSeconds(60);
        }

        // ============================================================
        // CREATE DIDIT VERIFICATION SESSION
        // ============================================================

        public async Task<DiditVerificationSessionResult>
            CreateVerificationSessionAsync(
                DiditVerificationSessionRequest request,
                CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_options.Enabled)
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit identity verification is disabled."
                };
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit API key is not configured."
                };
            }

            if (string.IsNullOrWhiteSpace(_options.WorkflowId))
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit WorkflowId is not configured."
                };
            }

            if (string.IsNullOrWhiteSpace(request.ReferenceId))
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "The SwiftCare verification reference is missing."
                };
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "The applicant email address is missing."
                };
            }

            var baseUrl =
                string.IsNullOrWhiteSpace(_options.BaseUrl)
                    ? "https://verification.didit.me/"
                    : _options.BaseUrl.TrimEnd('/') + "/";

            var endpoint =
                $"{baseUrl}v3/session/";

            // --------------------------------------------------------
            // DIDIT SESSION PAYLOAD
            // --------------------------------------------------------

            var payload = new
            {
                workflow_id = _options.WorkflowId,

                /*
                 * vendor_data is our own SwiftCare reference.
                 * Didit uses this to identify/reuse unfinished
                 * sessions for the same vendor reference.
                 */
                vendor_data = request.ReferenceId,

                /*
                 * Webhook callback will be configured once the
                 * public SwiftCare webhook URL is available.
                 *
                 * Leaving this empty for now allows us to first
                 * verify session creation and hosted verification.
                 */
                callback = _options.CallbackUrl,

                callback_method = "both",

                metadata = new
                {
                    source = "SwiftCare",
                    reference_id = request.ReferenceId
                },

                language = "en",

                /*
                 * IMPORTANT:
                 *
                 * SwiftCare sends its own branded email.
                 * Therefore Didit notification emails remain disabled.
                 */
                contact_details = new
                {
                    email = request.Email,
                    send_notification_emails = false,
                    email_lang = "en"
                },

                expected_details = new
                {
                    first_name = request.FirstName,
                    last_name = request.LastName,

                    date_of_birth =
                        request.DateOfBirth?
                            .ToString("yyyy-MM-dd"),

                    id_country =
                        request.CountryCode,

                    expected_document_types =
                        new[]
                        {
                            "ID",
                            "P"
                        }
                }
            };

            var json =
                JsonSerializer.Serialize(
                    payload,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.CamelCase
                    });

            _logger.LogInformation(
                "Creating Didit verification session. " +
                "Endpoint: {Endpoint}, " +
                "ReferenceId: {ReferenceId}, " +
                "WorkflowId: {WorkflowId}",
                endpoint,
                request.ReferenceId,
                _options.WorkflowId);

            _logger.LogDebug(
                "Didit session request payload: {Payload}",
                json);

            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    endpoint);

            httpRequest.Headers.Add(
                "x-api-key",
                _options.ApiKey);

            httpRequest.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            HttpResponseMessage response;

            try
            {
                response =
                    await _httpClient.SendAsync(
                        httpRequest,
                        cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Didit verification session creation was cancelled.");

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception while calling Didit Create Session API.");

                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        $"Could not connect to Didit: {ex.Message}"
                };
            }

            using (response)
            {
                var responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                // ----------------------------------------------------
                // VERY IMPORTANT DEBUG LOG
                // ----------------------------------------------------

                _logger.LogInformation(
                    "Didit Create Session response. " +
                    "HTTP {StatusCode} ({ReasonPhrase}). " +
                    "Body: {ResponseBody}",
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    responseBody);

                // ----------------------------------------------------
                // DIDIT ERROR
                // ----------------------------------------------------

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Didit Create Session API failed. " +
                        "HTTP {StatusCode}. " +
                        "Response: {ResponseBody}",
                        (int)response.StatusCode,
                        responseBody);

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            $"Didit returned HTTP " +
                            $"{(int)response.StatusCode}: " +
                            responseBody
                    };
                }

                // ----------------------------------------------------
                // DESERIALIZE DIDIT RESPONSE
                // ----------------------------------------------------

                DiditSessionResponse? diditResponse;

                try
                {
                    diditResponse =
                        JsonSerializer.Deserialize<DiditSessionResponse>(
                            responseBody,
                            JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Could not deserialize Didit response. " +
                        "Raw response: {ResponseBody}",
                        responseBody);

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            "Didit returned an invalid JSON response. " +
                            "Check the application logs for the raw response."
                    };
                }

                if (diditResponse == null)
                {
                    _logger.LogError(
                        "Didit returned an empty response body.");

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            "Didit returned an empty response."
                    };
                }

                // ----------------------------------------------------
                // LOG PARSED RESPONSE
                // ----------------------------------------------------

                _logger.LogInformation(
                    "Parsed Didit response. " +
                    "SessionId={SessionId}, " +
                    "SessionNumber={SessionNumber}, " +
                    "Url={Url}, " +
                    "Status={Status}, " +
                    "WorkflowId={WorkflowId}",
                    diditResponse.SessionId,
                    diditResponse.SessionNumber,
                    diditResponse.Url,
                    diditResponse.Status,
                    diditResponse.WorkflowId);

                // ----------------------------------------------------
                // SESSION ID REQUIRED
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        diditResponse.SessionId))
                {
                    _logger.LogError(
                        "Didit returned HTTP {StatusCode}, " +
                        "but session_id was missing. " +
                        "Raw response: {ResponseBody}",
                        (int)response.StatusCode,
                        responseBody);

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            "Didit did not return a session ID. " +
                            $"Raw response: {responseBody}"
                    };
                }

                // ----------------------------------------------------
                // HOSTED VERIFICATION URL REQUIRED
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        diditResponse.Url))
                {
                    _logger.LogError(
                        "Didit returned session {SessionId}, " +
                        "but verification URL was missing. " +
                        "Raw response: {ResponseBody}",
                        diditResponse.SessionId,
                        responseBody);

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            "Didit created a session but did not " +
                            "return a verification URL."
                    };
                }

                // ----------------------------------------------------
                // DIDIT SESSION SUCCESSFULLY CREATED
                // ----------------------------------------------------

                var result =
                    new DiditVerificationSessionResult
                    {
                        Success = true,

                        SessionId =
                            diditResponse.SessionId,

                        /*
                         * For now we use session_id as the provider
                         * verification ID because the Create Session
                         * response gives us the session identifier.
                         */
                        VerificationId =
                            diditResponse.SessionId,

                        VerificationUrl =
                            diditResponse.Url,

                        Status =
                            diditResponse.Status
                    };

                _logger.LogInformation(
                    "Didit verification session created successfully. " +
                    "SessionId={SessionId}, " +
                    "VerificationUrl={VerificationUrl}, " +
                    "ReferenceId={ReferenceId}",
                    diditResponse.SessionId,
                    diditResponse.Url,
                    request.ReferenceId);

                // ----------------------------------------------------
                // SEND SWIFTCARE EMAIL
                // ----------------------------------------------------

                try
                {
                    if (!string.IsNullOrWhiteSpace(
                            request.Email))
                    {
                        _logger.LogInformation(
                            "ABOUT TO SEND SWIFTCARE IDENTITY " +
                            "VERIFICATION EMAIL. " +
                            "Email={Email}, " +
                            "SessionId={SessionId}, " +
                            "VerificationUrl={VerificationUrl}",
                            request.Email,
                            diditResponse.SessionId,
                            diditResponse.Url);

                        var applicantName =
                            $"{request.FirstName} {request.LastName}"
                                .Trim();

                        var emailBody =
                            BuildIdentityVerificationEmailBody(
                                applicantName,
                                diditResponse.Url,
                                request.ReferenceId);

                        await _emailService.SendEmailAsync(
                            request.Email,
                            "SwiftCare Identity Verification Required",
                            emailBody,
                            "SwiftCare");

                        _logger.LogInformation(
                            "SWIFTCARE IDENTITY VERIFICATION EMAIL " +
                            "SENT SUCCESSFULLY. " +
                            "Email={Email}, " +
                            "SessionId={SessionId}",
                            request.Email,
                            diditResponse.SessionId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Didit session {SessionId} was created, " +
                            "but applicant email was empty. " +
                            "No SwiftCare email was sent.",
                            diditResponse.SessionId);
                    }
                }
                catch (Exception ex)
                {
                    /*
                     * IMPORTANT:
                     *
                     * Didit session creation already succeeded.
                     *
                     * We therefore DO NOT mark the verification as
                     * failed simply because the SwiftCare email failed.
                     */
                    _logger.LogError(
                        ex,
                        "Didit session {SessionId} was created successfully, " +
                        "but SwiftCare could not send the verification email.",
                        diditResponse.SessionId);

                    result.ErrorMessage =
                        "Didit verification session was created, " +
                        "but SwiftCare could not send the email invitation.";
                }

                return result;
            }
        }

        // ============================================================
        // RESEND EMAIL FOR EXISTING DIDIT SESSION
        // ============================================================

        public async Task<bool> ResendVerificationEmailAsync(
            string email,
            string applicantName,
            string verificationUrl,
            string verificationReference,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                _logger.LogWarning(
                    "Cannot resend Didit verification email " +
                    "because Didit is disabled.");

                return false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning(
                    "Cannot resend Didit verification email " +
                    "because applicant email is empty.");

                return false;
            }

            if (string.IsNullOrWhiteSpace(verificationUrl))
            {
                _logger.LogWarning(
                    "Cannot resend Didit verification email " +
                    "because verification URL is empty. " +
                    "Reference: {ReferenceId}",
                    verificationReference);

                return false;
            }

            if (string.IsNullOrWhiteSpace(verificationReference))
            {
                _logger.LogWarning(
                    "Cannot resend Didit verification email " +
                    "because verification reference is empty.");

                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var emailSubject =
                    "SwiftCare Identity Verification Required";

                var emailBody =
                    BuildIdentityVerificationEmailBody(
                        applicantName,
                        verificationUrl,
                        verificationReference);

                _logger.LogInformation(
                    "RESENDING SWIFTCARE IDENTITY VERIFICATION EMAIL. " +
                    "Recipient: {Email}, " +
                    "Reference: {ReferenceId}",
                    email,
                    verificationReference);

                await _emailService.SendEmailAsync(
                    email,
                    emailSubject,
                    emailBody,
                    "SwiftCare");

                _logger.LogInformation(
                    "SWIFTCARE IDENTITY VERIFICATION EMAIL " +
                    "RESENT SUCCESSFULLY. " +
                    "Recipient: {Email}, " +
                    "Reference: {ReferenceId}",
                    email,
                    verificationReference);

                return true;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "FAILED TO RESEND SWIFTCARE IDENTITY " +
                    "VERIFICATION EMAIL. " +
                    "Recipient: {Email}, " +
                    "Reference: {ReferenceId}",
                    email,
                    verificationReference);

                return false;
            }
        }

        // ============================================================
        // GET VERIFICATION RESULT
        // ============================================================

        public async Task<DiditVerificationResult?>
            GetVerificationResultAsync(
                string providerVerificationId,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(
                    providerVerificationId))
            {
                return null;
            }

            _logger.LogInformation(
                "Didit verification result requested for " +
                "provider verification ID {ProviderVerificationId}.",
                providerVerificationId);

            /*
             * Didit verification status will normally be processed
             * through the webhook.
             *
             * The webhook service will update:
             *
             * - Status
             * - DocumentVerified
             * - LivenessVerified
             * - FaceMatchVerified
             * - ProviderPersonId
             * - FailureReason
             */

            await Task.CompletedTask;

            return null;
        }

        // ============================================================
        // SWIFTCARE IDENTITY VERIFICATION EMAIL
        // ============================================================

        private static string BuildIdentityVerificationEmailBody(
            string applicantName,
            string verificationUrl,
            string verificationReference)
        {
            var safeApplicantName =
                System.Net.WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(applicantName)
                        ? "Applicant"
                        : applicantName);

            var safeVerificationUrl =
                System.Net.WebUtility.HtmlEncode(
                    verificationUrl);

            var safeReference =
                System.Net.WebUtility.HtmlEncode(
                    verificationReference);

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">

    <meta name=""viewport""
          content=""width=device-width, initial-scale=1.0"">

    <title>SwiftCare Identity Verification</title>
</head>

<body style=""
    margin:0;
    padding:0;
    background-color:#F0FDF4;
    font-family:Arial,Helvetica,sans-serif;
"">

<table width=""100%""
       cellpadding=""0""
       cellspacing=""0""
       border=""0""
       style=""background-color:#F0FDF4;padding:30px 15px;"">

    <tr>
        <td align=""center"">

            <table width=""600""
                   cellpadding=""0""
                   cellspacing=""0""
                   border=""0""
                   style=""
                       max-width:600px;
                       width:100%;
                       background:#ffffff;
                       border-radius:18px;
                       overflow:hidden;
                       border:1px solid #D5E7D6;
                       box-shadow:0 8px 30px rgba(27,94,32,0.12);
                   "">

                <!-- HEADER -->

                <tr>
                    <td style=""
                        background:#1B5E20;
                        padding:28px 30px;
                        text-align:center;
                    "">

                        <div style=""
                            color:#ffffff;
                            font-size:28px;
                            font-weight:bold;
                            letter-spacing:0.5px;
                        "">
                            SwiftCare
                        </div>

                        <div style=""
                            color:#DCEDC8;
                            font-size:14px;
                            margin-top:6px;
                        "">
                            Volunteer Management System
                        </div>

                    </td>
                </tr>

                <!-- CONTENT -->

                <tr>
                    <td style=""
                        padding:35px 35px 20px 35px;
                        color:#333333;
                    "">

                        <h2 style=""
                            margin:0 0 18px 0;
                            color:#1B5E20;
                            font-size:24px;
                        "">
                            Identity Verification Required
                        </h2>

                        <p style=""
                            margin:0 0 15px 0;
                            font-size:15px;
                            line-height:1.7;
                        "">
                            Hello <strong>{safeApplicantName}</strong>,
                        </p>

                        <p style=""
                            margin:0 0 15px 0;
                            font-size:15px;
                            line-height:1.7;
                        "">
                            As part of your SwiftCare volunteer onboarding,
                            you are required to complete an identity
                            verification process.
                        </p>

                        <p style=""
                            margin:0 0 20px 0;
                            font-size:15px;
                            line-height:1.7;
                        "">
                            The verification process will securely guide
                            you through the required identity document
                            and facial verification steps.
                        </p>

                        <!-- REFERENCE -->

                        <table width=""100%""
                               cellpadding=""0""
                               cellspacing=""0""
                               border=""0""
                               style=""
                                   background:#F0FDF4;
                                   border:1px solid #D5E7D6;
                                   border-radius:12px;
                                   margin-bottom:25px;
                               "">

                            <tr>
                                <td style=""
                                    padding:16px 18px;
                                    font-size:14px;
                                "">

                                    <strong style=""
                                        color:#1B5E20;
                                    "">
                                        Verification Reference
                                    </strong>

                                    <br />

                                    <span style=""
                                        color:#555555;
                                        font-family:monospace;
                                    "">
                                        {safeReference}
                                    </span>

                                </td>
                            </tr>

                        </table>

                        <!-- BUTTON -->

                        <div style=""
                            text-align:center;
                            margin:30px 0;
                        "">

                            <a href=""{safeVerificationUrl}""
                               target=""_blank""
                               style=""
                                   display:inline-block;
                                   background:#1B5E20;
                                   color:#ffffff;
                                   text-decoration:none;
                                   padding:15px 30px;
                                   border-radius:10px;
                                   font-size:16px;
                                   font-weight:bold;
                               "">

                                Start Identity Verification

                            </a>

                        </div>

                        <p style=""
                            margin:0 0 15px 0;
                            font-size:14px;
                            line-height:1.7;
                            color:#555555;
                        "">
                            Please complete the verification using the
                            button above. You may be asked to provide your
                            identity document and complete a facial
                            verification step.
                        </p>

                        <p style=""
                            margin:0 0 15px 0;
                            font-size:14px;
                            line-height:1.7;
                            color:#555555;
                        "">
                            If the button does not work, copy and paste
                            the following link into your browser:
                        </p>

                        <div style=""
                            background:#f7f7f7;
                            border-radius:8px;
                            padding:12px;
                            word-break:break-all;
                            font-size:12px;
                            color:#555555;
                        "">
                            {safeVerificationUrl}
                        </div>

                    </td>
                </tr>

                <!-- SECURITY NOTICE -->

                <tr>
                    <td style=""
                        padding:10px 35px 30px 35px;
                    "">

                        <table width=""100%""
                               cellpadding=""0""
                               cellspacing=""0""
                               border=""0""
                               style=""
                                   background:#FFFDE7;
                                   border:1px solid #E6E0A3;
                                   border-radius:10px;
                               "">

                            <tr>
                                <td style=""
                                    padding:15px 18px;
                                    font-size:13px;
                                    line-height:1.6;
                                    color:#555555;
                                "">

                                    <strong>
                                        Security Notice
                                    </strong>

                                    <br />

                                    SwiftCare will never ask you to send
                                    your identity document or verification
                                    information by replying to this email.

                                </td>
                            </tr>

                        </table>

                    </td>
                </tr>

                <!-- FOOTER -->

                <tr>
                    <td style=""
                        background:#124116;
                        padding:22px 30px;
                        text-align:center;
                        color:#DCEDC8;
                        font-size:12px;
                        line-height:1.6;
                    "">

                        <strong style=""color:#ffffff;"">
                            SwiftCare
                        </strong>

                        <br />

                        Volunteer Identity Verification

                        <br />

                        This is an automated message.
                        Please do not reply to this email.

                    </td>
                </tr>

            </table>

        </td>
    </tr>

</table>

</body>
</html>";
        }

        // ============================================================
        // DIDIT CREATE SESSION RESPONSE
        // ============================================================

        private sealed class DiditSessionResponse
        {
            [JsonPropertyName("session_id")]
            public string? SessionId { get; set; }

            [JsonPropertyName("session_number")]
            public int? SessionNumber { get; set; }

            [JsonPropertyName("session_token")]
            public string? SessionToken { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("vendor_data")]
            public string? VendorData { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("workflow_id")]
            public string? WorkflowId { get; set; }

            [JsonPropertyName("workflow_version")]
            public int? WorkflowVersion { get; set; }

            [JsonPropertyName("callback")]
            public string? Callback { get; set; }
        }
    }
}