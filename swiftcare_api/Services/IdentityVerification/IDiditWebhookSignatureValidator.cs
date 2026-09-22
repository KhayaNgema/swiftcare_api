namespace swiftcare_api.Services.IdentityVerification
{
    public interface IDiditWebhookSignatureValidator
    {
        bool IsTimestampValid(long timestamp);

        bool VerifyV2(
            string rawBody,
            string signature);

        bool VerifyRawBody(
            string rawBody,
            string signature);

        bool VerifySimple(
            long timestamp,
            string? sessionId,
            string? status,
            string? webhookType,
            string signature);
    }
}