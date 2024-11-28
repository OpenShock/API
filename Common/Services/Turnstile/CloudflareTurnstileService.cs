using System.Net;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;

namespace OpenShock.Common.Services.Turnstile;

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

    private static Error<CloduflareTurnstileError[]> CreateError(params ReadOnlySpan<CloduflareTurnstileError> errors)
    {
        return new Error<CloduflareTurnstileError[]>(errors.ToArray());
    }

    private static CloduflareTurnstileError MapCfError(string error)
    {
        return error switch
        {
            "missing-input-secret" => CloduflareTurnstileError.MissingSecret,
            "invalid-input-secret" => CloduflareTurnstileError.InvalidSecret,
            "missing-input-response" => CloduflareTurnstileError.MissingResponse,
            "invalid-input-response" => CloduflareTurnstileError.InvalidResponse,
            "bad-request" => CloduflareTurnstileError.BadRequest,
            "timeout-or-duplicate" => CloduflareTurnstileError.TimeoutOrDuplicate,
            "internal-error" => CloduflareTurnstileError.InternalServerError,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
        };
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, Error<CloduflareTurnstileError[]>>> VerifyUserResponseToken(
        string responseToken, IPAddress? remoteIpAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(responseToken)) return CreateError(CloduflareTurnstileError.MissingResponse);

#if DEBUG
        if (responseToken == "dev-bypass") return new Success();
#endif

        var formUrlValues = new Dictionary<string, string>
        {
            { "secret", _options.SecretKey },
            { "response", responseToken }
        };

        if (remoteIpAddress != null) formUrlValues["remoteip"] = remoteIpAddress.ToString();

        using var httpContent = new FormUrlEncodedContent(formUrlValues);

        using var httpResponse = await _httpClient.PostAsync(SiteVerifyEndpoint, httpContent, cancellationToken);
        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Turnstile error: {StatusCode} {ReasonPhrase}", httpResponse.StatusCode, httpResponse.ReasonPhrase);
            
            return CreateError(httpResponse.StatusCode == HttpStatusCode.BadRequest ? CloduflareTurnstileError.BadRequest : CloduflareTurnstileError.InternalServerError);
        }

        var response =  await httpResponse.Content.ReadFromJsonAsync<CloudflareTurnstileVerifyResponseDto>(cancellationToken);

        if (response.Success) return new Success();
        
        var errors = response.ErrorCodes.Select(MapCfError).ToArray();

        if (errors.All(err => err != CloduflareTurnstileError.InvalidResponse))
        {
            _logger.LogError("Turnstile error: {StatusCode} {ReasonPhrase}", httpResponse.StatusCode, string.Join(" ", errors.Select(err => err.ToString())));
        }

        return CreateError(errors);
    }
}

public readonly struct MissingInput;