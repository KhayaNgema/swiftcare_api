namespace swiftcare_api.Configuration
{
    public class DiditOptions
    {
        public const string SectionName = "Didit";

        public string BaseUrl { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string WebhookSecret { get; set; } = string.Empty;

        public string WorkflowId { get; set; } = string.Empty;

        public string CallbackUrl { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;
    }
}