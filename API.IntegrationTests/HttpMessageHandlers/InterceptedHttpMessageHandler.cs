using System.Net;
using System.Text.Json;
using System.Web;

namespace OpenShock.API.IntegrationTests.HttpMessageHandlers;

sealed class InterceptedHttpMessageHandler : DelegatingHandler
{
    private async Task<HttpResponseMessage> HandleCloudflareTurnstileRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var formData = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : string.Empty;
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
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        return responseMessage;
    }

    private Task<HttpResponseMessage> HandleMailJetApiHost(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return request.RequestUri switch
        {
            { Host: "challenges.cloudflare.com", AbsolutePath: "/turnstile/v0/siteverify" } => await HandleCloudflareTurnstileRequest(request, cancellationToken),
            { Host: "api.mailjet.com" } => await HandleMailJetApiHost(request, cancellationToken),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private class CloudflareTurnstileVerifyResponseDto
    {
        public bool Success { get; init; }
        public required string[] ErrorCodes { get; init; }
        public DateTime ChallengeTs { get; init; }
        public required string Hostname { get; init; }
        public required string Action { get; init; }
        public required string Cdata { get; init; }
    }
}