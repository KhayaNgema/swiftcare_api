using System.ComponentModel.DataAnnotations;

namespace swiftcare_api.Models.VolunteerOnboarding
{
    public class DiditWebhookLog
    {
        public int Id { get; set; }

        // Didit's stable event identifier.
        // This is our primary idempotency key.
        [Required]
        [MaxLength(100)]
        public string EventId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? WebhookType { get; set; }

        [MaxLength(100)]
        public string? SessionId { get; set; }

        [MaxLength(100)]
        public string? VendorData { get; set; }

        [MaxLength(100)]
        public string? Status { get; set; }

        [MaxLength(100)]
        public string? Environment { get; set; }

        public long Timestamp { get; set; }

        public DateTime ReceivedDate { get; set; }

        public DateTime? ProcessedDate { get; set; }

        public bool SignatureVerified { get; set; }

        public bool Processed { get; set; }

        public bool ProcessingFailed { get; set; }

        [MaxLength(4000)]
        public string? ProcessingError { get; set; }

        // Store the exact received payload for audit/debugging.
        public string RawBody { get; set; } = string.Empty;
    }
}