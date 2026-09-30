using System.Net;
using OpenShock.API.Options;
using OpenShock.Common.Extensions;
using OpenShock.Common.Results;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Services.Turnstile;

public sealed class CloudflareTurnstileService : ICloudflareTurnstileService
{
    private const string SiteVerifyEndpoint = "siteverify";

    private readonly HttpClient _httpClient;
    private readonly TurnstileOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CloudflareTurnstileService> _logger;

    public CloudflareTurnstileService(
        HttpClient httpClient,
        TurnstileOptions options,
        IHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CloudflareTurnstileService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private static CloudflareTurnstileError[] CreateError(params CloudflareTurnstileError[] errors) => errors;

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
    public async Task<SuccessOrError<CloudflareTurnstileError[]>> VerifyUserResponseTokenAsync(
        string responseToken, IPAddress? remoteIpAddress, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return new Success();
        
        // An admin-issued bypass token resolved earlier in the pipeline counts as a Turnstile pass
        // if it carries the Turnstile type. The middleware already bumped use counters; controllers
        // separately call IBypassTokenService.TryRecordUseAsync after auth so privileged-account
        // requests can be rejected and per-user cleanup can run.
        if (_httpContextAccessor.HttpContext?.IsBypassed(BypassTokenType.Turnstile) ?? false)
            return new Success();

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

        if (!errors.All(err => err.IsClientError()))
        {
            _logger.LogError("Turnstile verification failed: {Errors}", string.Join(" ", errors.Select(err => err.ToString())));
        }

        return CreateError(errors);
    }
};