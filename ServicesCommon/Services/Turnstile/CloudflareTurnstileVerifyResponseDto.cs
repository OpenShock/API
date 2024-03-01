using System.Text.Json.Serialization;

namespace OpenShock.ServicesCommon.Services.Turnstile;

public struct CloudflareTurnstileVerifyResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("challenge_ts")]
    public DateTime ChallengeTimeStamp { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("error-codes")]
    public IReadOnlyList<string> ErrorCodes { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("cdata")]
    public string? CData { get; set; }
}