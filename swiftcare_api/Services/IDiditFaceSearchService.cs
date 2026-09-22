//using Microsoft.AspNetCore.Http;
//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;

//namespace swiftcare_api.Services
//{
//    public interface IDiditFaceSearchService
//    {
//        // ============================================================
//        // SEARCH DIDIT'S VERIFIED / ONBOARDED FACE DATABASE
//        // ============================================================
//        //
//        // This service is for a directly supplied face image.
//        //
//        // It must search against the previously verified identities
//        // created during volunteer onboarding.
//        //
//        // It must NOT use previous SwiftCare face-login sessions as
//        // identity records.
//        //
//        // ============================================================

//        Task<DiditFaceSearchResult>
//            SearchFaceAsync(
//                IFormFile image,
//                CancellationToken cancellationToken = default);
//    }

//    // ================================================================
//    // DIDIT FACE SEARCH RESULT
//    // ================================================================

//    public class DiditFaceSearchResult
//    {
//        // ============================================================
//        // SUCCESS
//        // ============================================================

//        public bool Success { get; set; }

//        // ============================================================
//        // DIDIT SEARCH STATUS
//        // ============================================================

//        public string? Status { get; set; }

//        // ============================================================
//        // DIDIT REQUEST ID
//        // ============================================================

//        public string? RequestId { get; set; }

//        // ============================================================
//        // TOTAL MATCHES
//        // ============================================================

//        public int TotalMatchCount { get; set; }

//        // ============================================================
//        // WHETHER A SWIFTCARE ONBOARDING IDENTITY MATCHED
//        // ============================================================

//        public bool FaceSearchMatched { get; set; }

//        // ============================================================
//        // STRONGEST ONBOARDING MATCH
//        // ============================================================
//        //
//        // Expected:
//        //
//        // SWIFTCARE-ID-123
//        //
//        // ============================================================

//        public string? MatchedVendorData { get; set; }

//        // ============================================================
//        // STRONGEST MATCH SIMILARITY
//        // ============================================================

//        public double? MatchSimilarity { get; set; }

//        // ============================================================
//        // STRONGEST MATCH STATUS
//        // ============================================================

//        public string? MatchedStatus { get; set; }

//        // ============================================================
//        // ALL VALID ONBOARDING MATCHES
//        // ============================================================

//        public List<DiditFaceSearchMatch>
//            Matches
//        { get; set; }
//            = new List<DiditFaceSearchMatch>();

//        // ============================================================
//        // ERROR
//        // ============================================================

//        public string? ErrorMessage { get; set; }
//    }

//    // ================================================================
//    // DIDIT FACE SEARCH MATCH
//    // ================================================================

//    public class DiditFaceSearchMatch
//    {
//        // ============================================================
//        // SWIFTCARE ONBOARDING VENDOR DATA
//        // ============================================================
//        //
//        // Expected:
//        //
//        // SWIFTCARE-ID-{VolunteerIdentityVerificationId}
//        //
//        // ============================================================

//        public string? VendorData { get; set; }

//        // ============================================================
//        // FACE SIMILARITY
//        // ============================================================

//        public double Similarity { get; set; }

//        // ============================================================
//        // DIDIT STATUS
//        // ============================================================

//        public string? Status { get; set; }

//        // ============================================================
//        // DIDIT ALLOWLIST
//        // ============================================================

//        public bool IsAllowlisted { get; set; }

//        // ============================================================
//        // DIDIT BLOCKLIST
//        // ============================================================

//        public bool IsBlocklisted { get; set; }
//    }
//}