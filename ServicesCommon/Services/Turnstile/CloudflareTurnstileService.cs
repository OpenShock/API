using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using System.Net;

namespace OpenShock.ServicesCommon.Services.Turnstile;

public sealed class CloudflareTurnstileService : ICloudflareTurnstileService
{
    public const string BaseUrl = "https://challenges.cloudflare.com/turnstile/v0/";
    private const string SiteVerifyEndpoint = "siteverify";

    private readonly HttpClient _httpClient;
    private readonly CloudflareTurnstileOptions _options;
    private readonly ILogger<CloudflareTurnstileService> _logger;

    public CloudflareTurnstileService(HttpClient httpClient, IOptions<CloudflareTurnstileOptions> options,
        ILogger<CloudflareTurnstileService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, MissingInput, Error, Error<IReadOnlyList<string>>>> VerifyUserResponseToken(
        string responseToken, IPAddress? remoteIpAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(responseToken)) return new MissingInput();

#if DEBUG
        if (responseToken == "dev-bypass") return new Success();
#endif

        var formUrlValues = new Dictionary<string, string>
        {
            { "secret", _options.SecretKey },
            { "response", responseToken }
        };

        if (remoteIpAddress != null) formUrlValues["remoteip"] = remoteIpAddress.MapToIPv4().ToString();

        var httpContent = new FormUrlEncodedContent(formUrlValues);


        using var httpResponse = await _httpClient.PostAsync(SiteVerifyEndpoint, httpContent, cancellationToken);
        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Turnstile error: {StatusCode} {ReasonPhrase}", httpResponse.StatusCode,
                httpResponse.ReasonPhrase);
            return new Error();
        }

        var response =
            await httpResponse.Content.ReadFromJsonAsync<CloudflareTurnstileVerifyResponseDto>(
                cancellationToken: cancellationToken);

        if (response.Success) return new Success();

        return new Error<IReadOnlyList<string>>(response.ErrorCodes);
    }
}

public readonly struct MissingInput;