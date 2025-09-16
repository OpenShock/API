using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Options;
using OpenShock.API.OAuth;
using OpenShock.Common.Utils;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    // Optional: tag common EventIds for easy filtering
    private static class Evt
    {
        public static readonly EventId HandoffStart          = new(1000, nameof(HandoffStart));
        public static readonly EventId FlowValidationOk      = new(1001, nameof(FlowValidationOk));
        public static readonly EventId FlowValidationFail    = new(1002, nameof(FlowValidationFail));
        public static readonly EventId ProviderMismatch      = new(1003, nameof(ProviderMismatch));
        public static readonly EventId FetchConnection       = new(1004, nameof(FetchConnection));
        public static readonly EventId ConnectionFound       = new(1005, nameof(ConnectionFound));
        public static readonly EventId ConnectionMissing     = new(1006, nameof(ConnectionMissing));
        public static readonly EventId MustBeAnonymous       = new(1007, nameof(MustBeAnonymous));
        public static readonly EventId MustBeAuthenticated   = new(1008, nameof(MustBeAuthenticated));
        public static readonly EventId AddConnectionAttempt  = new(1009, nameof(AddConnectionAttempt));
        public static readonly EventId AddConnectionFailed   = new(1010, nameof(AddConnectionFailed));
        public static readonly EventId AddConnectionOk       = new(1011, nameof(AddConnectionOk));
        public static readonly EventId CreateSession         = new(1012, nameof(CreateSession));
        public static readonly EventId CookieDomainMissing   = new(1013, nameof(CookieDomainMissing));
        public static readonly EventId FlowCookieSignOut     = new(1014, nameof(FlowCookieSignOut));
        public static readonly EventId RedirectingFrontend   = new(1015, nameof(RedirectingFrontend));
        public static readonly EventId HandoffUnhandledFlow  = new(1016, nameof(HandoffUnhandledFlow));
        public static readonly EventId HandoffCompleted      = new(1017, nameof(HandoffCompleted));
        public static readonly EventId HandoffException      = new(1018, nameof(HandoffException));
        
        public static readonly EventId ValidateStart         = new(1100, nameof(ValidateStart));
        public static readonly EventId TempCookieMissing     = new(1101, nameof(TempCookieMissing));
        public static readonly EventId SchemeMissing         = new(1102, nameof(SchemeMissing));
        public static readonly EventId FlowParseFail         = new(1103, nameof(FlowParseFail));
        public static readonly EventId ExternalIdMissing     = new(1104, nameof(ExternalIdMissing));
        public static readonly EventId ValidateSuccess       = new(1105, nameof(ValidateSuccess));
    }

    /// <summary>
    /// Handoff after provider callback. Decides next step (create, link, or direct sign-in).
    /// </summary>
    /// <remarks>
    /// Reads the temp OAuth flow principal (flow cookie set by middleware).
    /// If a matching connection exists -> signs in and redirects home.
    /// Otherwise -> redirects frontend to continue the chosen flow.
    /// </remarks>
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/handoff")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OAuthHandOff(
        [FromRoute] string provider,
        [FromServices] IOAuthConnectionService connectionService,
        [FromServices] FrontendOptions frontendOptions,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        // Create a structured logging scope that follows this request
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceIdentifier"] = HttpContext.TraceIdentifier,
            ["ProviderRoute"]   = provider
        });

        _logger.LogInformation(Evt.HandoffStart, "OAuth handoff started for provider '{ProviderRoute}'.", provider);

        try
        {
        var result = await ValidateOAuthFlowAsync();
        if (!result.TryPickT0(out var auth, out var error))
        {
            _logger.LogWarning(Evt.FlowValidationFail,
                "OAuth flow validation failed: {Error}. Redirecting to frontend error.",
                error.ToString());

            return error switch
            {
                OAuthValidationError.FlowStateMissing => RedirectFrontendError("OAuthFlowNotStarted"),
                _ => RedirectFrontendError("InternalError")
            };
        }

        // add useful details to the scope once we have them
        using var scope2 = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["Flow"]                = auth.Flow.ToString(),
            ["ProviderFlow"]        = auth.Provider,
            ["ExternalAccountHash"] = HashForLogs(auth.ExternalAccountId),
            ["ExternalName"]        = auth.ExternalAccountDisplayName ?? auth.ExternalAccountName
        });

        _logger.LogInformation(Evt.FlowValidationOk,
            "OAuth flow validated. Flow={Flow}, Provider={ProviderFlow}, ExternalAccountHash={ExternalAccountHash}.",
            auth.Flow, auth.Provider, HashForLogs(auth.ExternalAccountId));

        // 1) Defense-in-depth: ensure the flow’s provider matches the route
        if (!string.Equals(auth.Provider, provider, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(Evt.ProviderMismatch,
                "Provider mismatch. Flow provider '{FlowProvider}' vs route '{ProviderRoute}'.",
                auth.Provider, provider);

            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            _logger.LogDebug(Evt.FlowCookieSignOut, "Signed out flow cookie after provider mismatch.");

            return RedirectFrontendError("providerMismatch");
        }

        _logger.LogDebug(Evt.FetchConnection,
            "Fetching connection for Provider={ProviderRoute}, ExternalAccountHash={ExternalAccountHash}.",
            provider, HashForLogs(auth.ExternalAccountId));

        var connection = await connectionService
            .GetByProviderExternalIdAsync(provider, auth.ExternalAccountId, cancellationToken);

        if (connection is null)
        {
            _logger.LogInformation(Evt.ConnectionMissing,
                "No existing connection found for provider {ProviderRoute}, external={ExternalAccountHash}.",
                provider, HashForLogs(auth.ExternalAccountId));
        }
        else
        {
            _logger.LogInformation(Evt.ConnectionFound,
                "Existing connection found. UserId={UserId}, provider={ProviderRoute}.",
                connection.UserId, provider);
        }

        switch (auth.Flow)
        {
            case OAuthFlow.LoginOrCreate:
            {
                if (User.HasOpenShockUserIdentity())
                {
                    _logger.LogWarning(Evt.MustBeAnonymous,
                        "LoginOrCreate flow requires anonymous user, but current request is authenticated. Aborting.");

                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    _logger.LogDebug(Evt.FlowCookieSignOut, "Signed out flow cookie (must be anonymous).");

                    return RedirectFrontendError("mustBeAnonymous");
                }

                if (connection is null)
                {
                    _logger.LogInformation(Evt.RedirectingFrontend,
                        "No connection; redirecting to CREATE flow on frontend for provider {ProviderRoute}.",
                        provider);

                    return RedirectFrontendPath($"/oauth/{Uri.EscapeDataString(provider)}/create");
                }

                // Direct sign-in
                var domain = GetCurrentCookieDomain();
                if (string.IsNullOrEmpty(domain))
                {
                    _logger.LogError(Evt.CookieDomainMissing,
                        "Cookie domain resolution failed during direct sign-in.");

                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    _logger.LogDebug(Evt.FlowCookieSignOut, "Signed out flow cookie after domain failure.");

                    return RedirectFrontendError("internalError");
                }

                _logger.LogInformation(Evt.CreateSession,
                    "Creating session for UserId={UserId}, Domain={Domain}.",
                    connection.UserId, domain);

                await CreateSession(connection.UserId, domain);

                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                _logger.LogDebug(Evt.FlowCookieSignOut, "Flow cookie cleared after successful sign-in.");

                _logger.LogInformation(Evt.RedirectingFrontend, "Redirecting to /home after direct sign-in.");
                return RedirectFrontendPath("/home");
            }

            case OAuthFlow.Link:
            {
                if (!User.TryGetAuthenticatedOpenShockUserId(out var userId))
                {
                    _logger.LogWarning(Evt.MustBeAuthenticated,
                        "Link flow requires authenticated user, but request is anonymous.");

                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    _logger.LogDebug(Evt.FlowCookieSignOut, "Signed out flow cookie (must be authenticated).");

                    return RedirectFrontendError("mustBeAuthenticated");
                }

                using var scope3 = _logger.BeginScope(new Dictionary<string, object?>
                {
                    ["AuthenticatedUserId"] = userId
                });

                if (connection is not null)
                {
                    var status = connection.UserId == userId ? "alreadyLinked" : "linkedToAnotherAccount";
                    _logger.LogWarning(Evt.RedirectingFrontend,
                        "Link attempt found existing connection. ExistingUserId={ExistingUserId}, RequestedUserId={RequestedUserId}, Status={Status}.",
                        connection.UserId, userId, status);

                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    _logger.LogDebug(Evt.FlowCookieSignOut, "Flow cookie cleared after link conflict.");

                    return RedirectFrontendConnections(status);
                }

                _logger.LogInformation(Evt.AddConnectionAttempt,
                    "Attempting to add connection. UserId={UserId}, Provider={ProviderRoute}, ExternalAccountHash={ExternalAccountHash}, Display='{DisplayName}'.",
                    userId, provider, HashForLogs(auth.ExternalAccountId),
                    auth.ExternalAccountDisplayName ?? auth.ExternalAccountName);

                var ok = await connectionService.TryAddConnectionAsync(
                    userId,
                    provider,
                    auth.ExternalAccountId,
                    auth.ExternalAccountDisplayName ?? auth.ExternalAccountName,
                    cancellationToken);

                if (!ok)
                {
                    _logger.LogError(Evt.AddConnectionFailed,
                        "Adding connection FAILED for UserId={UserId}, Provider={ProviderRoute}.",
                        userId, provider);

                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    _logger.LogDebug(Evt.FlowCookieSignOut, "Flow cookie cleared after add-connection failure.");

                    return RedirectFrontendConnections("linkFailed");
                }

                _logger.LogInformation(Evt.AddConnectionOk,
                    "Connection added. Proceeding to sign-in for UserId={UserId}.",
                    userId);

                var domain = GetCurrentCookieDomain();
                if (string.IsNullOrEmpty(domain))
                {
                    _logger.LogError(Evt.CookieDomainMissing,
                        "Cookie domain resolution failed during post-link sign-in.");

                    await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                    _logger.LogDebug(Evt.FlowCookieSignOut, "Flow cookie cleared after domain failure.");

                    return RedirectFrontendConnections("internalError");
                }

                _logger.LogInformation(Evt.CreateSession,
                    "Creating session after link for UserId={UserId}, Domain={Domain}.",
                    userId, domain);

                await CreateSession(userId, domain);

                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                _logger.LogDebug(Evt.FlowCookieSignOut, "Flow cookie cleared after successful link+sign-in.");

                _logger.LogInformation(Evt.RedirectingFrontend,
                    "Redirecting to Connections page with 'linked' status.");
                return RedirectFrontendConnections("linked");
            }

            default:
            {
                _logger.LogError(Evt.HandoffUnhandledFlow,
                    "Unhandled OAuth flow type: {Flow}.", auth.Flow);

                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                _logger.LogDebug(Evt.FlowCookieSignOut, "Flow cookie cleared after unhandled flow.");

                return RedirectFrontendError("internalError");
            }
        }
        }
        catch (Exception ex)
        {
            _logger.LogError(Evt.HandoffException, ex,
                "Unhandled exception during OAuth handoff for provider {ProviderRoute}.", provider);

            // best-effort cleanup
            try
            {
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                _logger.LogDebug(Evt.FlowCookieSignOut, "Flow cookie cleared in exception handler.");
            }
            catch (Exception signOutEx)
            {
                _logger.LogWarning(Evt.HandoffException,
                    signOutEx, "Exception while clearing flow cookie in exception handler.");
            }

            return RedirectFrontendError("internalError");
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(Evt.HandoffCompleted,
                "OAuth handoff finished in {ElapsedMs} ms for provider {ProviderRoute}.",
                sw.ElapsedMilliseconds, provider);
        }

        // --- helpers ---

        RedirectResult RedirectFrontendPath(string path)
        {
            var dest = new Uri(frontendOptions.BaseUrl, path).ToString();
            _logger.LogDebug(Evt.RedirectingFrontend, "Redirecting to '{Redirect}'.", dest);
            return Redirect(dest);
        }

        RedirectResult RedirectFrontendError(string errorType)
            => RedirectFrontendPath($"/oauth/error?error={Uri.EscapeDataString(errorType)}");
        
        RedirectResult RedirectFrontendConnections(string statusType)
            => RedirectFrontendPath($"/settings/connections?status={Uri.EscapeDataString(statusType)}");
    }

    /// <summary>Redacts sensitive IDs by hashing; safe for logs.</summary>
    private static string HashForLogs(string value)
    {
        if (string.IsNullOrEmpty(value)) return "<null>";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..16]; // short prefix
    }
}
