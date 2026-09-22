using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using swiftcare_api.Data;
using swiftcare_api.Models;
using swiftcare_api.Models.VolunteerOnboarding;
using swiftcare_api.Services;

using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace swiftcare_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<UserBaseModel> _signInManager;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        // ============================================================
        // DIDIT FACE LOGIN
        //
        // Uses:
        //
        // Passive Liveness
        //       +
        // Automatic Didit 1:N Face Search
        //
        // NO EMAIL
        // NO PHONE
        // NO USERNAME
        // NO FACE IMAGE UPLOAD
        //
        // The user is identified entirely by their live face.
        // ============================================================

        private readonly IDiditFaceLoginService
            _diditFaceLoginService;

        private readonly swiftcare_apiDbContext
            _dbContext;

        public AuthController(
            SignInManager<UserBaseModel> signInManager,
            UserManager<UserBaseModel> userManager,
            ILogger<AuthController> logger,
            IConfiguration configuration,
            IDiditFaceLoginService diditFaceLoginService,
            swiftcare_apiDbContext dbContext)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;

            _diditFaceLoginService =
                diditFaceLoginService;

            _dbContext = dbContext;
        }

        // ============================================================
        // LOGIN REQUEST
        //
        // THIS IS THE EXISTING PASSWORD LOGIN.
        //
        // DO NOT CHANGE.
        // ============================================================

        public class LoginRequest
        {
            [Required(
                ErrorMessage =
                    "The email or phone field is required.")]
            public string EmailOrPhone { get; set; }

            [Required(
                ErrorMessage =
                    "The password field is required")]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }

        // ============================================================
        // EXISTING PASSWORD LOGIN
        //
        // DO NOT CHANGE
        // ============================================================

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x =>
                        x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp =>
                            kvp.Value.Errors
                                .Select(e =>
                                    e.ErrorMessage)
                                .ToArray()
                    );

                return BadRequest(new
                {
                    message =
                        "Validation failed",

                    errors
                });
            }

            var isEmail =
                new EmailAddressAttribute()
                    .IsValid(request.EmailOrPhone);

            var user = isEmail
                ? await _userManager
                    .FindByEmailAsync(
                        request.EmailOrPhone)

                : await _userManager
                    .Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.PhoneNumber ==
                            request.EmailOrPhone);

            if (user == null)
            {
                return NotFound(
                    "User account does not exist.");
            }

            if (!user.EmailConfirmed)
            {
                return StatusCode(
                    406,
                    "User email address is not confirmed. Please check your emails");
            }

            var result =
                await _signInManager
                    .PasswordSignInAsync(
                        user.UserName,
                        request.Password,
                        request.RememberMe,
                        lockoutOnFailure: false
                    );

            if (!result.Succeeded)
            {
                return StatusCode(
                    406,
                    "Incorrect email/phone/password.");
            }

            var roles =
                await _userManager
                    .GetRolesAsync(user);

            var role =
                roles?.FirstOrDefault()
                ?? "Customer";

            var token =
                GenerateJwtToken(
                    user,
                    role);

            return Ok(new
            {
                message =
                    "Login successful",

                userName =
                    user.UserName,

                email =
                    user.Email,

                token,

                role
            });
        }

        // ============================================================
        // FACE LOGIN
        //
        // NATIVE DIDIT SDK FLOW
        //
        // Flutter sends:
        //
        // POST /api/Auth/face-login
        //
        // {}
        //
        // SwiftCare:
        //
        //     1. Creates unique face-login attempt
        //     2. Creates Didit Passive Liveness session
        //     3. Returns Didit session token
        //
        // Flutter then passes the session token to the
        // native Didit Flutter SDK.
        //
        // Didit:
        //
        //     Passive Liveness
        //            ↓
        //     Automatic 1:N Face Search
        //            ↓
        //     Finds previously enrolled person
        //
        // NO EMAIL
        // NO PHONE
        // NO USERNAME
        // NO FACE IMAGE
        // ============================================================

        [HttpPost("face-login")]
        [AllowAnonymous]
        public async Task<IActionResult> FaceLogin(
            CancellationToken cancellationToken)
        {
            // ========================================================
            // CREATE UNIQUE FACE LOGIN ATTEMPT REFERENCE
            // ========================================================

            var loginAttemptReference =
                $"SWIFTCARE-FACE-LOGIN-{Guid.NewGuid():N}";

            _logger.LogInformation(
                "Starting anonymous Didit native face login. " +
                "LoginAttemptReference: {LoginAttemptReference}",
                loginAttemptReference);

            try
            {
                // ====================================================
                // CREATE DIDIT FACE LOGIN SESSION
                //
                // NO EMAIL
                // NO PHONE
                // NO USERNAME
                // ====================================================

                var faceLoginSession =
                    await _diditFaceLoginService
                        .CreateFaceLoginSessionAsync(
                            loginAttemptReference,
                            cancellationToken);

                // ====================================================
                // DIDIT SERVICE FAILED
                // ====================================================

                if (!faceLoginSession.Success)
                {
                    _logger.LogError(
                        "Didit face-login session creation failed. " +
                        "LoginAttemptReference: {LoginAttemptReference}, " +
                        "SessionId: {SessionId}, " +
                        "Status: {Status}, " +
                        "Error: {Error}",
                        loginAttemptReference,
                        faceLoginSession.SessionId,
                        faceLoginSession.Status,
                        faceLoginSession.ErrorMessage);

                    return StatusCode(
                        StatusCodes.Status502BadGateway,
                        new
                        {
                            success = false,

                            message =
                                "Unable to start face verification.",

                            error =
                                faceLoginSession.ErrorMessage,

                            status =
                                faceLoginSession.Status,

                            sessionId =
                                faceLoginSession.SessionId
                        });
                }

                // ====================================================
                // SESSION CREATED
                //
                // DO NOT LOG THE SESSION TOKEN.
                // ====================================================

                _logger.LogInformation(
                    "Didit passive-liveness face-login session created. " +
                    "LoginAttemptReference: {LoginAttemptReference}, " +
                    "SessionId: {SessionId}, " +
                    "SessionTokenPresent: {SessionTokenPresent}, " +
                    "VerificationUrlPresent: {VerificationUrlPresent}, " +
                    "Status: {Status}",
                    loginAttemptReference,
                    faceLoginSession.SessionId,
                    !string.IsNullOrWhiteSpace(
                        faceLoginSession.SessionToken),
                    !string.IsNullOrWhiteSpace(
                        faceLoginSession.VerificationUrl),
                    faceLoginSession.Status);

                // ====================================================
                // NATIVE FLUTTER SDK RESPONSE
                //
                // sessionToken is the important new property.
                //
                // verificationUrl is retained temporarily for
                // backwards compatibility but native Flutter does
                // not need it.
                // ====================================================

                return Ok(new
                {
                    success = true,

                    message =
                        "Face verification started.",

                    sessionId =
                        faceLoginSession.SessionId,

                    sessionToken =
                        faceLoginSession.SessionToken,

                    verificationUrl =
                        faceLoginSession.VerificationUrl,

                    status =
                        faceLoginSession.Status
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Didit face-login request was cancelled. " +
                    "LoginAttemptReference: {LoginAttemptReference}",
                    loginAttemptReference);

                return StatusCode(
                    StatusCodes.Status408RequestTimeout,
                    new
                    {
                        success = false,

                        message =
                            "Face verification request timed out or was cancelled."
                    });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error while creating Didit face-login session. " +
                    "LoginAttemptReference: {LoginAttemptReference}. " +
                    "Message: {Message}",
                    loginAttemptReference,
                    ex.Message);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,

                        message =
                            "SwiftCare could not communicate with Didit.",

                        error =
                            ex.Message
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while starting Didit face login. " +
                    "LoginAttemptReference: {LoginAttemptReference}. " +
                    "Message: {Message}",
                    loginAttemptReference,
                    ex.Message);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,

                        message =
                            "An unexpected error occurred while starting face verification.",

                        error =
                            ex.Message
                    });
            }
        }

        // ============================================================
        // FACE LOGIN STATUS
        //
        // Flutter:
        //
        // GET
        // /api/Auth/face-login/status/{sessionId}
        //
        // Didit:
        //
        //     Passive Liveness
        //            ↓
        //     Automatic 1:N Face Search
        //
        // We do NOT know the user before this point.
        //
        // Once Didit returns the face-search match, we locate the
        // original verified SwiftCare identity.
        // ============================================================

        [HttpGet("face-login/status/{sessionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> FaceLoginStatus(
     string sessionId,
     CancellationToken cancellationToken)
        {
            // ============================================================
            // VALIDATE SESSION ID
            // ============================================================

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Didit session ID is required."
                });
            }

            sessionId = sessionId.Trim();

            // ============================================================
            // GET DIDIT DECISION
            // ============================================================

            var decision =
                await _diditFaceLoginService
                    .GetFaceLoginDecisionAsync(
                        sessionId,
                        cancellationToken);

            if (decision == null)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message =
                            "Unable to retrieve face verification result."
                    });
            }

            if (!decision.Success)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message =
                            "Unable to retrieve face verification result.",
                        error =
                            decision.ErrorMessage
                    });
            }

            // ============================================================
            // DIDIT HAS NOT FINISHED
            // ============================================================

            if (!IsDiditFinished(decision.Status))
            {
                return Ok(new
                {
                    success = true,
                    authenticated = false,
                    completed = false,

                    status =
                        decision.Status,

                    livenessVerified =
                        decision.LivenessVerified,

                    faceSearchMatched =
                        decision.FaceSearchMatched,

                    message =
                        "Face verification is still processing."
                });
            }

            // ============================================================
            // DIDIT DID NOT APPROVE
            // ============================================================

            if (!IsDiditApproved(decision.Status))
            {
                _logger.LogWarning(
                    "Didit face-login session {SessionId} " +
                    "was not approved. Status: {Status}",
                    sessionId,
                    decision.Status);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    status =
                        decision.Status,

                    livenessVerified =
                        decision.LivenessVerified,

                    faceSearchMatched =
                        decision.FaceSearchMatched,

                    message =
                        "Face verification was not approved.",

                    reason =
                        decision.FailureReason
                });
            }

            // ============================================================
            // REQUIRE PASSIVE LIVENESS
            // ============================================================

            if (!decision.LivenessVerified)
            {
                _logger.LogWarning(
                    "Didit session {SessionId} was approved " +
                    "but passive liveness was not verified.",
                    sessionId);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    status =
                        decision.Status,

                    livenessVerified = false,

                    faceSearchMatched =
                        decision.FaceSearchMatched,

                    message =
                        "Live-person verification was not completed successfully."
                });
            }

            // ============================================================
            // REQUIRE 1:N FACE SEARCH MATCH
            // ============================================================

            if (!decision.FaceSearchMatched)
            {
                _logger.LogWarning(
                    "Didit session {SessionId} was approved " +
                    "and liveness passed, but no matching enrolled " +
                    "SwiftCare face was found.",
                    sessionId);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    status =
                        decision.Status,

                    livenessVerified =
                        decision.LivenessVerified,

                    faceSearchMatched = false,

                    message =
                        "The live face could not be matched to a registered SwiftCare volunteer."
                });
            }

            // ============================================================
            // FIND MATCHED SWIFTCARE IDENTITY
            // ============================================================

            VolunteerIdentityVerification? verification = null;

            // ============================================================
            // OPTION 1:
            //
            // MATCHED DIDIT SESSION ID
            //
            // We use this only to locate the original verification.
            //
            // IMPORTANT:
            // Do NOT apply all verification requirements here.
            // ============================================================

            if (!string.IsNullOrWhiteSpace(
                    decision.MatchedSessionId))
            {
                _logger.LogInformation(
                    "Attempting SwiftCare identity lookup using " +
                    "MatchedSessionId. " +
                    "LoginSession: {LoginSession}, " +
                    "MatchedSessionId: {MatchedSessionId}",
                    sessionId,
                    decision.MatchedSessionId);

                verification =
                    await _dbContext
                        .VolunteerIdentityVerifications
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            v =>
                                v.Provider == "Didit" &&
                                v.ProviderSessionId ==
                                    decision.MatchedSessionId,
                            cancellationToken);

                if (verification != null)
                {
                    _logger.LogInformation(
                        "MatchedSessionId successfully mapped to " +
                        "SwiftCare verification. " +
                        "VerificationId: {VerificationId}",
                        verification.Id);
                }
            }

            // ============================================================
            // OPTION 2:
            //
            // MATCHED VENDOR DATA
            //
            // Example:
            //
            // SWIFTCARE-ID-1008
            //
            // This is the most important mapping for your current log.
            // ============================================================

            if (verification == null &&
                !string.IsNullOrWhiteSpace(
                    decision.MatchedVendorData))
            {
                var verificationId =
                    ExtractVerificationId(
                        decision.MatchedVendorData);

                if (verificationId.HasValue)
                {
                    _logger.LogInformation(
                        "Didit returned SwiftCare matched vendor data. " +
                        "VendorData: {VendorData}, " +
                        "ExtractedVerificationId: {VerificationId}",
                        decision.MatchedVendorData,
                        verificationId.Value);

                    verification =
                        await _dbContext
                            .VolunteerIdentityVerifications
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                v =>
                                    v.Id ==
                                        verificationId.Value &&
                                    v.Provider == "Didit",
                                cancellationToken);

                    if (verification != null)
                    {
                        _logger.LogInformation(
                            "SwiftCare verification found from " +
                            "MatchedVendorData. " +
                            "VerificationId: {VerificationId}, " +
                            "Status: {Status}, " +
                            "DocumentVerified: {DocumentVerified}, " +
                            "LivenessVerified: {LivenessVerified}, " +
                            "FaceMatchVerified: {FaceMatchVerified}, " +
                            "FaceIdentityEnrolled: {FaceIdentityEnrolled}, " +
                            "VolunteerId: {VolunteerId}",
                            verification.Id,
                            verification.Status,
                            verification.DocumentVerified,
                            verification.LivenessVerified,
                            verification.FaceMatchVerified,
                            verification.FaceIdentityEnrolled,
                            verification.VolunteerId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Didit returned MatchedVendorData " +
                            "{VendorData}, but no SwiftCare " +
                            "VolunteerIdentityVerification exists " +
                            "with Id {VerificationId}.",
                            decision.MatchedVendorData,
                            verificationId.Value);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Didit returned MatchedVendorData {VendorData}, " +
                        "but SwiftCare could not extract a verification ID.",
                        decision.MatchedVendorData);
                }
            }

            // ============================================================
            // COULD NOT MAP DIDIT FACE TO SWIFTCARE
            // ============================================================

            if (verification == null)
            {
                _logger.LogWarning(
                    "Didit face search matched a person but " +
                    "SwiftCare could not map the result. " +
                    "LoginSession: {LoginSession}, " +
                    "MatchedSession: {MatchedSession}, " +
                    "MatchedVendorData: {MatchedVendorData}",
                    sessionId,
                    decision.MatchedSessionId,
                    decision.MatchedVendorData);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "The biometric identity could not be matched to a registered SwiftCare volunteer."
                });
            }

            // ============================================================
            // NOW VALIDATE THE SWIFTCARE IDENTITY
            // ============================================================

            if (verification.Status !=
                    VolunteerIdentityVerificationStatus.Verified)
            {
                _logger.LogWarning(
                    "Matched SwiftCare verification {VerificationId} " +
                    "is not Verified. Status: {Status}",
                    verification.Id,
                    verification.Status);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "The biometric identity has not completed SwiftCare verification."
                });
            }

            // ============================================================
            // REQUIRE DOCUMENT VERIFICATION
            // ============================================================

            if (!verification.DocumentVerified)
            {
                _logger.LogWarning(
                    "Matched SwiftCare verification {VerificationId} " +
                    "has not completed document verification.",
                    verification.Id);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "The identity document has not been verified."
                });
            }

            // ============================================================
            // REQUIRE LIVENESS VERIFICATION
            // ============================================================

            if (!verification.LivenessVerified)
            {
                _logger.LogWarning(
                    "Matched SwiftCare verification {VerificationId} " +
                    "has not completed liveness verification.",
                    verification.Id);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "Liveness verification has not been completed."
                });
            }

            // ============================================================
            // REQUIRE FACE MATCH VERIFICATION
            // ============================================================

            if (!verification.FaceMatchVerified)
            {
                _logger.LogWarning(
                    "Matched SwiftCare verification {VerificationId} " +
                    "has not completed face-match verification.",
                    verification.Id);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "Face verification has not been completed."
                });
            }

            // ============================================================
            // REQUIRE FACE IDENTITY ENROLLMENT
            // ============================================================

            if (!verification.FaceIdentityEnrolled)
            {
                _logger.LogWarning(
                    "Matched SwiftCare verification {VerificationId} " +
                    "has not been enrolled for face recognition.",
                    verification.Id);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "This identity has not been enrolled for biometric login."
                });
            }

            // ============================================================
            // FIND APPLICANT ID
            //
            // IMPORTANT:
            // VolunteerIdentityVerification.VolunteerId currently contains
            // the Applicant Identity ID created during the application/
            // onboarding process.
            //
            // It does NOT contain the final Volunteer Identity ID.
            //
            // Therefore we first resolve the Applicant.
            // ============================================================

            var applicantId =
                verification.VolunteerId;

            if (string.IsNullOrWhiteSpace(applicantId))
            {
                _logger.LogWarning(
                    "Verification {VerificationId} has no ApplicantId.",
                    verification.Id);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "This biometric identity is not linked to a SwiftCare applicant."
                });
            }

            // ============================================================
            // FIND APPLICANT
            // ============================================================

            var applicant =
                await _userManager
                    .FindByIdAsync(applicantId);

            if (applicant == null)
            {
                _logger.LogWarning(
                    "VolunteerIdentityVerification {VerificationId} " +
                    "references ApplicantId {ApplicantId}, but the " +
                    "Identity applicant could not be found.",
                    verification.Id,
                    applicantId);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "The applicant account could not be found."
                });
            }

            // ============================================================
            // MAKE SURE THIS IDENTITY IS AN APPLICANT
            // ============================================================
            //
            // Because UserBaseModel uses TPH inheritance, querying
            // Applicants ensures that the Identity record is actually
            // an Applicant and not some other Identity user.
            // ============================================================

            var applicantProfile =
                await _dbContext
                    .Applicants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a =>
                            a.Id == applicant.Id,
                        cancellationToken);

            if (applicantProfile == null)
            {
                _logger.LogWarning(
                    "Identity user {ApplicantId} exists, but no Applicant " +
                    "record could be found for verification {VerificationId}.",
                    applicant.Id,
                    verification.Id);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "The applicant account could not be found."
                });
            }

            // ============================================================
            // FIND VOLUNTEER ONBOARDING
            //
            // THIS IS THE IMPORTANT LINK.
            //
            // VolunteerIdentityVerification
            //        ↓
            // ApplicantId
            //        ↓
            // VolunteerOnboarding.ApplicantId
            // ============================================================

            var onboarding =
                await _dbContext
                    .VolunteerOnboardings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        o =>
                            o.VolunteerOnboardingId ==
                                verification.VolunteerOnboardingId &&
                            o.ApplicantId ==
                                applicant.Id,
                        cancellationToken);

            if (onboarding == null)
            {
                _logger.LogWarning(
                    "No VolunteerOnboarding could be found for " +
                    "VerificationId {VerificationId}, " +
                    "ApplicantId {ApplicantId}, " +
                    "OnboardingId {OnboardingId}.",
                    verification.Id,
                    applicant.Id,
                    verification.VolunteerOnboardingId);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "The volunteer onboarding record could not be found."
                });
            }

            // ============================================================
            // FIND VOLUNTEER
            //
            // Volunteer does NOT need ApplicantId.
            //
            // The Volunteer is linked to the onboarding using:
            //
            // Volunteer.VolunteerOnboardingId
            // ============================================================

            var verifiedVolunteer =
                await _dbContext
                    .Volunteers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        v =>
                            v.VolunteerOnboardingId ==
                                onboarding.VolunteerOnboardingId,
                        cancellationToken);

            if (verifiedVolunteer == null)
            {
                _logger.LogWarning(
                    "Applicant {ApplicantId} has onboarding {OnboardingId}, " +
                    "but no Volunteer record could be found.",
                    applicant.Id,
                    onboarding.VolunteerOnboardingId);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "The volunteer account could not be found."
                });
            }

            // ============================================================
            // VOLUNTEER MUST BE ACTIVE
            // ============================================================

            if (verifiedVolunteer.Status !=
                VolunteerStatus.Active)
            {
                _logger.LogWarning(
                    "Face login rejected because volunteer {VolunteerId} " +
                    "for Applicant {ApplicantId} is not active. " +
                    "Status: {Status}",
                    verifiedVolunteer.Id,
                    applicant.Id,
                    verifiedVolunteer.Status);

                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "This volunteer account is not active."
                });
            }

            // ============================================================
            // EMAIL CONFIRMATION
            //
            // The authenticated Identity user is the Applicant Identity
            // because that is the Identity account used throughout the
            // onboarding process.
            // ============================================================

            if (!applicant.EmailConfirmed)
            {
                return Unauthorized(new
                {
                    success = false,
                    authenticated = false,
                    completed = true,

                    message =
                        "This user's account has not completed email confirmation."
                });
            }

            // ============================================================
            // GET ROLE
            // ============================================================

            var roles =
                await _userManager
                    .GetRolesAsync(applicant);

            var role =
                roles?.FirstOrDefault()
                ?? "Volunteer";

            // ============================================================
            // GENERATE JWT
            //
            // Keep the Applicant Identity ID as the authenticated
            // Identity/User ID because that is the Identity account
            // associated with the onboarding/verification.
            //
            // The Volunteer record is used to validate that this
            // applicant has actually been activated as a volunteer.
            // ============================================================

            var token =
                GenerateJwtToken(
                    applicant,
                    role);

            // ============================================================
            // SUCCESS
            // ============================================================

            _logger.LogInformation(
                "SwiftCare passive-liveness face login successful. " +
                "ApplicantId: {ApplicantId}, " +
                "VolunteerId: {VolunteerId}, " +
                "OnboardingId: {OnboardingId}, " +
                "VerificationId: {VerificationId}, " +
                "DiditLoginSessionId: {SessionId}, " +
                "MatchedDiditSessionId: {MatchedSessionId}",
                applicant.Id,
                verifiedVolunteer.Id,
                onboarding.VolunteerOnboardingId,
                verification.Id,
                sessionId,
                decision.MatchedSessionId);

            return Ok(new
            {
                success = true,

                authenticated = true,

                completed = true,

                message =
                    "Face login successful.",

                // Identity account
                userId =
                    applicant.Id,

                userName =
                    applicant.UserName,

                email =
                    applicant.Email,

                firstName =
                    applicant.FirstName,

                lastName =
                    applicant.LastName,

                // Actual activated volunteer
                volunteerId =
                    verifiedVolunteer.Id,

                volunteerNumber =
                    verifiedVolunteer.VolunteerNumber,

                onboardingId =
                    onboarding.VolunteerOnboardingId,

                token,

                role
            });
        }
        // ============================================================
        // DIDIT STATUS HELPERS
        // ============================================================

        private static bool IsDiditApproved(
            string? status)
        {
            if (string.IsNullOrWhiteSpace(
                    status))
            {
                return false;
            }

            return status.Equals(
                       "Approved",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDiditFinished(
            string? status)
        {
            if (string.IsNullOrWhiteSpace(
                    status))
            {
                return false;
            }

            return
                status.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase)

                ||

                status.Equals(
                    "Declined",
                    StringComparison.OrdinalIgnoreCase)

                ||

                status.Equals(
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase)

                ||

                status.Equals(
                    "Expired",
                    StringComparison.OrdinalIgnoreCase)

                ||

                status.Equals(
                    "Abandoned",
                    StringComparison.OrdinalIgnoreCase)

                ||

                status.Equals(
                    "Kyc Expired",
                    StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // EXTRACT SWIFTCARE VERIFICATION ID
        //
        // Example:
        //
        // SWIFTCARE-ID-1012
        //
        // Returns:
        //
        // 1012
        //
        // IMPORTANT:
        //
        // This method is ONLY used against MatchedVendorData.
        // ============================================================

        private static int? ExtractVerificationId(
            string vendorData)
        {
            const string prefix =
                "SWIFTCARE-ID-";

            if (string.IsNullOrWhiteSpace(
                    vendorData))
            {
                return null;
            }

            if (!vendorData.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var idPart =
                vendorData
                    .Substring(
                        prefix.Length)
                    .Trim();

            if (int.TryParse(
                    idPart,
                    out var verificationId))
            {
                return verificationId;
            }

            return null;
        }

        // ============================================================
        // EXISTING JWT GENERATION
        //
        // DO NOT CHANGE
        // ============================================================

        private string GenerateJwtToken(
            UserBaseModel user,
            string role)
        {
            var utcNow =
                DateTime.UtcNow;

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id),

                new Claim(
                    ClaimTypes.Name,
                    user.UserName),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    role),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(utcNow)
                        .ToUnixTimeSeconds()
                        .ToString(),
                    ClaimValueTypes.Integer64)
            };

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _configuration["Jwt:Key"]));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer:
                        _configuration["Jwt:Issuer"],

                    audience:
                        _configuration["Jwt:Audience"],

                    claims:
                        claims,

                    notBefore:
                        utcNow,

                    expires:
                        utcNow.AddHours(1),

                    signingCredentials:
                        credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}