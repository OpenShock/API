using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace OpenShock.API.IntegrationTests.HttpMessageHandlers;

sealed class InterceptedHttpMessageHandler : DelegatingHandler
{
    private async Task<HttpResponseMessage> HandleCloudflareTurnstileRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var formData = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var parsedForm = HttpUtility.ParseQueryString(formData);
        var responseToken = parsedForm["response"];

        var responseDto = responseToken switch
        {
            "valid-token" => new CloudflareTurnstileVerifyResponseDto
            {
                Success = true,
                ErrorCodes = [],
                ChallengeTs = DateTime.UtcNow,
                Hostname = "validhost",
                Action = "validaction",
                Cdata = ""
            },
            "invalid-token" => new CloudflareTurnstileVerifyResponseDto
            {
                Success = false,
                ErrorCodes = ["invalid-input-response"],
                ChallengeTs = DateTime.UtcNow,
                Hostname = "invalidhost",
                Action = "invalidaction",
                Cdata = ""
            },
            "expired-token" => new CloudflareTurnstileVerifyResponseDto
            {
                Success = false,
                ErrorCodes = ["timeout-or-duplicate"],
                ChallengeTs = DateTime.UtcNow,
                Hostname = "expiredhost",
                Action = "expiredaction",
                Cdata = ""
            },
            _ => new CloudflareTurnstileVerifyResponseDto
            {
                Success = false,
                ErrorCodes = ["bad-request"],
                ChallengeTs = DateTime.UtcNow,
                Hostname = "unknownhost",
                Action = "unknownaction",
                Cdata = ""
            }
        };

        var responseJson = JsonSerializer.Serialize(responseDto);

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        return responseMessage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return request.RequestUri switch
        {
            { Host: "challenges.cloudflare.com", AbsolutePath: "/turnstile/v0/siteverify" } => await HandleCloudflareTurnstileRequest(request, cancellationToken),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private sealed class CloudflareTurnstileVerifyResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }
        [JsonPropertyName("error-codes")]
        public required string[] ErrorCodes { get; init; }
        [JsonPropertyName("challenge_ts")]
        public DateTime ChallengeTs { get; init; }
        [JsonPropertyName("hostname")]
        public required string Hostname { get; init; }
        [JsonPropertyName("action")]
        public required string Action { get; init; }
        [JsonPropertyName("cdata")]
        public required string Cdata { get; init; }
    }
}