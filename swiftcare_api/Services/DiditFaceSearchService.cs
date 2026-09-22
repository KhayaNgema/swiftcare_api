//using System.Globalization;
//using System.Net.Http.Headers;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace swiftcare_api.Services
//{
//    public class DiditFaceSearchService : IDiditFaceSearchService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly IConfiguration _configuration;
//        private readonly ILogger<DiditFaceSearchService> _logger;

//        public DiditFaceSearchService(
//            HttpClient httpClient,
//            IConfiguration configuration,
//            ILogger<DiditFaceSearchService> logger)
//        {
//            _httpClient = httpClient;
//            _configuration = configuration;
//            _logger = logger;

//            var baseUrl =
//                _configuration["Didit:BaseUrl"];

//            if (string.IsNullOrWhiteSpace(baseUrl))
//            {
//                baseUrl = "https://verification.didit.me/";
//            }

//            if (!baseUrl.EndsWith("/"))
//            {
//                baseUrl += "/";
//            }

//            _httpClient.BaseAddress = new Uri(baseUrl);

//            _httpClient.Timeout =
//                TimeSpan.FromSeconds(60);
//        }

//        public async Task<DiditFaceSearchResult> SearchFaceAsync(
//            IFormFile image,
//            CancellationToken cancellationToken = default)
//        {
//            if (image == null || image.Length == 0)
//            {
//                return new DiditFaceSearchResult
//                {
//                    Success = false,
//                    ErrorMessage = "Face image is required."
//                };
//            }

//            var apiKey =
//                _configuration["Didit:ApiKey"];

//            if (string.IsNullOrWhiteSpace(apiKey))
//            {
//                return new DiditFaceSearchResult
//                {
//                    Success = false,
//                    ErrorMessage =
//                        "Didit API key is not configured."
//                };
//            }

//            try
//            {
//                using var content =
//                    new MultipartFormDataContent();

//                await using var imageStream =
//                    image.OpenReadStream();

//                using var imageContent =
//                    new StreamContent(imageStream);

//                imageContent.Headers.ContentType =
//                    new MediaTypeHeaderValue(
//                        string.IsNullOrWhiteSpace(image.ContentType)
//                            ? "image/jpeg"
//                            : image.ContentType);

//                content.Add(
//                    imageContent,
//                    "user_image",
//                    image.FileName);

//                // 70 is Didit's documented default.
//                // We can adjust this later after testing
//                // your real volunteer images.
//                content.Add(
//                    new StringContent("80"),
//                    "similarity_threshold");

//                content.Add(
//                    new StringContent("false"),
//                    "allow_multiple_faces");

//                using var request =
//                    new HttpRequestMessage(
//                        HttpMethod.Post,
//                        "v3/face-search/");

//                request.Headers.Add(
//                    "x-api-key",
//                    apiKey);

//                request.Content = content;

//                _logger.LogInformation(
//                    "Sending face-login image to Didit Face Search.");

//                using var response =
//                    await _httpClient.SendAsync(
//                        request,
//                        cancellationToken);

//                var responseBody =
//                    await response.Content.ReadAsStringAsync(
//                        cancellationToken);

//                _logger.LogInformation(
//                    "Didit Face Search response: {StatusCode} {Body}",
//                    (int)response.StatusCode,
//                    responseBody);

//                if (!response.IsSuccessStatusCode)
//                {
//                    return new DiditFaceSearchResult
//                    {
//                        Success = false,
//                        ErrorMessage =
//                            $"Didit Face Search returned {(int)response.StatusCode}: {responseBody}"
//                    };
//                }

//                var responseObject =
//                    JsonSerializer.Deserialize<DiditFaceSearchResponse>(
//                        responseBody,
//                        new JsonSerializerOptions
//                        {
//                            PropertyNameCaseInsensitive = true
//                        });

//                if (responseObject == null)
//                {
//                    return new DiditFaceSearchResult
//                    {
//                        Success = false,
//                        ErrorMessage =
//                            "Didit returned an invalid Face Search response."
//                    };
//                }

//                var faceSearch =
//                    responseObject.FaceSearch;

//                if (faceSearch == null)
//                {
//                    return new DiditFaceSearchResult
//                    {
//                        Success = true,
//                        Status = responseObject.Status,
//                        RequestId = responseObject.RequestId,
//                        TotalMatches = 0
//                    };
//                }

//                var matches =
//                    faceSearch.Matches?
//                        .Select(match => new DiditFaceMatch
//                        {
//                            SessionId =
//                                match.SessionId,

//                            VendorData =
//                                match.VendorData,

//                            SimilarityPercentage =
//                                match.SimilarityPercentage,

//                            VerificationDate =
//                                match.VerificationDate,

//                            IsBlocklisted =
//                                match.IsBlocklisted
//                        })
//                        .ToList()
//                    ?? new List<DiditFaceMatch>();

//                return new DiditFaceSearchResult
//                {
//                    Success = true,

//                    Status =
//                        responseObject.Status ??
//                        faceSearch.Status,

//                    RequestId =
//                        responseObject.RequestId ??
//                        faceSearch.RequestId,

//                    TotalMatches =
//                        faceSearch.TotalMatches,

//                    Matches =
//                        matches
//                };
//            }
//            catch (OperationCanceledException)
//            {
//                throw;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(
//                    ex,
//                    "Error while communicating with Didit Face Search.");

//                return new DiditFaceSearchResult
//                {
//                    Success = false,
//                    ErrorMessage =
//                        "An error occurred while processing the face."
//                };
//            }
//        }

//        private sealed class DiditFaceSearchResponse
//        {
//            [JsonPropertyName("request_id")]
//            public string? RequestId { get; set; }

//            [JsonPropertyName("status")]
//            public string? Status { get; set; }

//            [JsonPropertyName("face_search")]
//            public DiditFaceSearchPayload? FaceSearch { get; set; }

//            // Some Didit responses may expose matches
//            // directly, so we also support that shape.
//            [JsonPropertyName("total_matches")]
//            public int? DirectTotalMatches { get; set; }

//            [JsonPropertyName("matches")]
//            public List<DiditFaceMatchResponse>? DirectMatches { get; set; }
//        }

//        private sealed class DiditFaceSearchPayload
//        {
//            [JsonPropertyName("request_id")]
//            public string? RequestId { get; set; }

//            [JsonPropertyName("status")]
//            public string? Status { get; set; }

//            [JsonPropertyName("total_matches")]
//            public int TotalMatches { get; set; }

//            [JsonPropertyName("matches")]
//            public List<DiditFaceMatchResponse>? Matches { get; set; }
//        }

//        private sealed class DiditFaceMatchResponse
//        {
//            [JsonPropertyName("session_id")]
//            public string? SessionId { get; set; }

//            [JsonPropertyName("vendor_data")]
//            public string? VendorData { get; set; }

//            [JsonPropertyName("similarity_percentage")]
//            public double SimilarityPercentage { get; set; }

//            [JsonPropertyName("similarity_score")]
//            public double? SimilarityScore { get; set; }

//            [JsonPropertyName("verification_date")]
//            public DateTime? VerificationDate { get; set; }

//            [JsonPropertyName("is_blocklisted")]
//            public bool IsBlocklisted { get; set; }
//        }
//    }
//}