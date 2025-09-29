using System.Net;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using OpenShock.API.Options;
using OpenShock.Common.Options;

namespace OpenShock.API.Services.Turnstile;

public sealed class CloudflareTurnstileService : ICloudflareTurnstileService
{
    private const string SiteVerifyEndpoint = "siteverify";

    private readonly HttpClient _httpClient;
    private readonly TurnstileOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CloudflareTurnstileService> _logger;

    public CloudflareTurnstileService(HttpClient httpClient, TurnstileOptions options, IHostEnvironment environment, ILogger<CloudflareTurnstileService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    private static Error<CloudflareTurnstileError[]> CreateError(params ReadOnlySpan<CloudflareTurnstileError> errors)
    {
        return new Error<CloudflareTurnstileError[]>(errors.ToArray());
    }

    private static CloudflareTurnstileError MapCfError(string error)
    {
        return error switch
        {
            "missing-input-secret" => CloudflareTurnstileError.MissingSecret,
            "invalid-input-secret" => CloudflareTurnstileError.InvalidSecret,
            "missing-input-response" => CloudflareTurnstileError.MissingResponse,
            "invalid-input-response" => CloudflareTurnstileError.InvalidResponse,
            "bad-request" => CloudflareTurnstileError.BadRequest,
            "timeout-or-duplicate" => CloudflareTurnstileError.TimeoutOrDuplicate,
            "internal-error" => CloudflareTurnstileError.InternalServerError,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
        };
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, Error<CloudflareTurnstileError[]>>> VerifyUserResponseTokenAsync(
        string responseToken, IPAddress? remoteIpAddress, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return new Success();
        
        if (string.IsNullOrEmpty(responseToken)) return CreateError(CloudflareTurnstileError.MissingResponse);

        if (_environment.IsDevelopment() && responseToken == "dev-bypass")
        {
            return new Success();
        }
        
        var formUrlValues = new Dictionary<string, string>
        {
            { "secret", _options.SecretKey },
            { "response", responseToken }
        };

        if (remoteIpAddress is not null) formUrlValues["remoteip"] = remoteIpAddress.ToString();

        using var httpContent = new FormUrlEncodedContent(formUrlValues);

        using var httpResponse = await _httpClient.PostAsync(SiteVerifyEndpoint, httpContent, cancellationToken);
        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Turnstile error: {StatusCode} {ReasonPhrase}", httpResponse.StatusCode, httpResponse.ReasonPhrase);
            
            return CreateError(httpResponse.StatusCode == HttpStatusCode.BadRequest ? CloudflareTurnstileError.BadRequest : CloudflareTurnstileError.InternalServerError);
        }

        var response =  await httpResponse.Content.ReadFromJsonAsync<CloudflareTurnstileVerifyResponseDto>(cancellationToken);

        if (response.Success) return new Success();
        
        var errors = response.ErrorCodes.Select(MapCfError).ToArray();

        if (errors.All(err => err != CloudflareTurnstileError.InvalidResponse))
        {
            _logger.LogError("Turnstile error: {StatusCode} {ReasonPhrase}", httpResponse.StatusCode, string.Join(" ", errors.Select(err => err.ToString())));
        }

        return CreateError(errors);
    }
};