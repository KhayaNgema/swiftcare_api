using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using swiftcare_api.Configuration;

namespace swiftcare_api.Services.IdentityVerification
{
    public class DiditWebhookSignatureValidator
        : IDiditWebhookSignatureValidator
    {
        private readonly DiditOptions _options;

        public DiditWebhookSignatureValidator(
            IOptions<DiditOptions> options)
        {
            _options = options.Value;
        }

        public bool IsTimestampValid(long timestamp)
        {
            if (timestamp <= 0)
            {
                return false;
            }

            var now =
                DateTimeOffset.Now.ToUnixTimeSeconds();

            var difference =
                Math.Abs(now - timestamp);

            return difference <= 300;
        }

        public bool VerifyV2(
            string rawBody,
            string signature)
        {
            if (string.IsNullOrWhiteSpace(
                    _options.WebhookSecret))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return false;
            }

            try
            {
                using var document =
                    JsonDocument.Parse(rawBody);

                var canonicalJson =
                    CanonicalizeJson(
                        document.RootElement);

                var expectedSignature =
                    ComputeHmac(
                        canonicalJson,
                        _options.WebhookSecret);

                return FixedTimeEquals(
                    expectedSignature,
                    signature);
            }
            catch
            {
                return false;
            }
        }

        public bool VerifyRawBody(
            string rawBody,
            string signature)
        {
            if (string.IsNullOrWhiteSpace(
                    _options.WebhookSecret))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return false;
            }

            try
            {
                var expectedSignature =
                    ComputeHmac(
                        rawBody,
                        _options.WebhookSecret);

                return FixedTimeEquals(
                    expectedSignature,
                    signature);
            }
            catch
            {
                return false;
            }
        }

        public bool VerifySimple(
            long timestamp,
            string? sessionId,
            string? status,
            string? webhookType,
            string signature)
        {
            if (string.IsNullOrWhiteSpace(
                    _options.WebhookSecret))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            var value =
                $"{timestamp}:" +
                $"{sessionId ?? string.Empty}:" +
                $"{status ?? string.Empty}:" +
                $"{webhookType ?? string.Empty}";

            var expectedSignature =
                ComputeHmac(
                    value,
                    _options.WebhookSecret);

            return FixedTimeEquals(
                expectedSignature,
                signature);
        }

        private static string ComputeHmac(
            string value,
            string secret)
        {
            using var hmac =
                new HMACSHA256(
                    Encoding.UTF8.GetBytes(secret));

            var hash =
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(value));

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }

        private static bool FixedTimeEquals(
            string expected,
            string supplied)
        {
            try
            {
                var expectedBytes =
                    Convert.FromHexString(
                        expected.Trim());

                var suppliedBytes =
                    Convert.FromHexString(
                        supplied.Trim());

                if (expectedBytes.Length !=
                    suppliedBytes.Length)
                {
                    return false;
                }

                return CryptographicOperations
                    .FixedTimeEquals(
                        expectedBytes,
                        suppliedBytes);
            }
            catch
            {
                return false;
            }
        }

        private static string CanonicalizeJson(
            JsonElement element)
        {
            var builder =
                new StringBuilder();

            WriteCanonicalJson(
                element,
                builder);

            return builder.ToString();
        }

        private static void WriteCanonicalJson(
            JsonElement element,
            StringBuilder builder)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    {
                        builder.Append('{');

                        var properties =
                            element
                                .EnumerateObject()
                                .OrderBy(
                                    x => x.Name,
                                    StringComparer.Ordinal)
                                .ToList();

                        for (
                            var i = 0;
                            i < properties.Count;
                            i++)
                        {
                            if (i > 0)
                            {
                                builder.Append(',');
                            }

                            WriteString(
                                properties[i].Name,
                                builder);

                            builder.Append(':');

                            WriteCanonicalJson(
                                properties[i].Value,
                                builder);
                        }

                        builder.Append('}');

                        break;
                    }

                case JsonValueKind.Array:
                    {
                        builder.Append('[');

                        var index = 0;

                        foreach (
                            var item in
                            element.EnumerateArray())
                        {
                            if (index++ > 0)
                            {
                                builder.Append(',');
                            }

                            WriteCanonicalJson(
                                item,
                                builder);
                        }

                        builder.Append(']');

                        break;
                    }

                case JsonValueKind.String:
                    {
                        WriteString(
                            element.GetString()
                                ?? string.Empty,
                            builder);

                        break;
                    }

                case JsonValueKind.Number:
                    {
                        WriteCanonicalNumber(
                            element,
                            builder);

                        break;
                    }

                case JsonValueKind.True:
                    {
                        builder.Append("true");
                        break;
                    }

                case JsonValueKind.False:
                    {
                        builder.Append("false");
                        break;
                    }

                case JsonValueKind.Null:
                    {
                        builder.Append("null");
                        break;
                    }

                default:
                    {
                        builder.Append(
                            element.GetRawText());

                        break;
                    }
            }
        }

        private static void WriteCanonicalNumber(
            JsonElement element,
            StringBuilder builder)
        {
            var raw =
                element.GetRawText();

            if (
                !raw.Contains('.') &&
                !raw.Contains('e') &&
                !raw.Contains('E'))
            {
                builder.Append(raw);
                return;
            }

            if (decimal.TryParse(
                    raw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var decimalValue))
            {
                if (
                    decimal.Truncate(
                        decimalValue)
                    == decimalValue)
                {
                    builder.Append(
                        decimalValue.ToString(
                            "0",
                            CultureInfo.InvariantCulture));

                    return;
                }

                builder.Append(
                    decimalValue.ToString(
                        "0.###############################",
                        CultureInfo.InvariantCulture));

                return;
            }

            if (double.TryParse(
                    raw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var doubleValue))
            {
                if (
                    double.IsFinite(doubleValue) &&
                    doubleValue ==
                    Math.Truncate(doubleValue))
                {
                    builder.Append(
                        Math.Truncate(doubleValue)
                            .ToString(
                                "0",
                                CultureInfo.InvariantCulture));

                    return;
                }

                builder.Append(
                    doubleValue.ToString(
                        "G17",
                        CultureInfo.InvariantCulture));

                return;
            }

            builder.Append(raw);
        }

        private static void WriteString(
            string value,
            StringBuilder builder)
        {
            var encoded =
                JsonSerializer.Serialize(
                    value,
                    new JsonSerializerOptions
                    {
                        Encoder =
                            JavaScriptEncoder
                                .UnsafeRelaxedJsonEscaping
                    });

            builder.Append(encoded);
        }
    }
}