using System.Text.Json.Serialization;

namespace OpenShock.ServicesCommon.Services.Turnstile;

public readonly struct CloudflareTurnstileVerifyResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("challenge_ts")]
    public DateTime ChallengeTimeStamp { get; init; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; init; }

    [JsonPropertyName("error-codes")]
    public IReadOnlyList<string> ErrorCodes { get; init; }

    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("cdata")]
    public string? CData { get; init; }
}