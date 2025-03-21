using System.Net;
using System.Text.Json;
using System.Web;

namespace OpenShock.API.IntegrationTests.HttpMessageHandlers;

sealed class CloudflareTurnstileHttpMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is null || !request.RequestUri.AbsoluteUri.EndsWith("siteverify"))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

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

    private class CloudflareTurnstileVerifyResponseDto
    {
        public bool Success { get; set; }
        public string[] ErrorCodes { get; set; }
        public DateTime ChallengeTs { get; set; }
        public string Hostname { get; set; }
        public string Action { get; set; }
        public string Cdata { get; set; }
    }
}