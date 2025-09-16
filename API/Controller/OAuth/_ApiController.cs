using System.Diagnostics;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OpenShock.API.OAuth;
using OpenShock.API.Services.Account;
using OpenShock.Common;
using OpenShock.Common.Authentication;

namespace OpenShock.API.Controller.OAuth;

/// <summary>
/// OAuth management endpoints (provider listing, authorize, data handoff).
/// </summary>
[ApiController]
[Tags("OAuth")]
[ApiVersion("1")]
[Route("/{version:apiVersion}/oauth")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionCookie), AllowAnonymous] // Tries to authenticate a user session, but doesnt block anonymous
public sealed partial class OAuthController : OpenShockControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(IAccountService accountService, IAuthenticationSchemeProvider schemeProvider, ILogger<OAuthController> logger)
    {
        _accountService = accountService;
        _schemeProvider = schemeProvider;
        _logger = logger;
    }

    private enum OAuthValidationError
    {
        FlowStateMissing,
        FlowDataMissingOrInvalid,
    }

    /// <summary>
    /// Validates: provider exists, temp cookie auth present, scheme matches, flow parsable.
    /// On success returns ValidatedFlowContext; on failure returns IActionResult with proper problem details.
    /// </summary>
    private async Task<OneOf<ValidatedFlowContext, OAuthValidationError>> ValidateOAuthFlowAsync()
    {
        var sw = Stopwatch.StartNew();
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceIdentifier"] = HttpContext.TraceIdentifier,
            ["Path"]            = HttpContext.Request.Path.ToString()
        });

        _logger.LogInformation(Evt.ValidateStart, "Validating OAuth flow from temp cookie.");

        // 1) authenticate temp cookie
        var auth = await HttpContext.AuthenticateAsync(OAuthConstants.FlowScheme);
        if (!auth.Succeeded || auth.Principal is null || auth.Ticket is null)
        {
            _logger.LogWarning(Evt.TempCookieMissing,
                "Temp cookie auth failed or missing. Succeeded={Succeeded}, Principal={HasPrincipal}, Ticket={HasTicket}.",
                auth.Succeeded, auth.Principal is not null, auth.Ticket is not null);

            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            _logger.LogDebug(Evt.FlowCookieSignOut, "Cleared flow cookie after failed/missing auth.");
            return OAuthValidationError.FlowStateMissing;
        }

        // Useful debug: identities/claims counts without leaking values
        var identitiesCount = auth.Principal.Identities.Count();
        var claimsCount = auth.Principal.Claims.Count();
        var propsCount = auth.Properties.Items.Count;

        _logger.LogDebug("Temp cookie authenticated. Identities={Identities}, Claims={Claims}, Properties={Props}.",
            identitiesCount, claimsCount, propsCount);

        // 2) scheme/provider check - prefer the ticket's scheme over a magic Item
        if (!auth.Properties.Items.TryGetValue(".AuthScheme", out var actualScheme) || string.IsNullOrWhiteSpace(actualScheme))
        {
            _logger.LogError(Evt.SchemeMissing,
                "OAuth scheme missing/invalid in auth properties. PropertyKeys=[{Keys}]",
                string.Join(",", auth.Properties.Items.Keys));

            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            _logger.LogDebug(Evt.FlowCookieSignOut, "Cleared flow cookie after missing scheme.");
            return OAuthValidationError.FlowDataMissingOrInvalid;
        }

        // 3) parse flow from properties
        if (!auth.Properties.Items.TryGetValue(OAuthConstants.ItemKeyFlowType, out var flowStr) ||
            !Enum.TryParse<OAuthFlow>(flowStr, true, out var flow))
        {
            _logger.LogError(Evt.FlowParseFail,
                "OAuth flow missing/invalid. Raw='{FlowRaw}'. Keys=[{Keys}]",
                flowStr, string.Join(",", auth.Properties?.Items?.Keys ?? Array.Empty<string>()));

            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            _logger.LogDebug(Evt.FlowCookieSignOut, "Cleared flow cookie after flow parse failure.");
            return OAuthValidationError.FlowDataMissingOrInvalid;
        }
        
        // 4) fetch id of external user
        var externalId = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(externalId))
        {
            _logger.LogError(Evt.ExternalIdMissing,
                "External account identifier claim is missing. AvailableClaims=[{ClaimTypes}]",
                string.Join(",", auth.Principal.Claims.Select(c => c.Type).Distinct()));

            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            _logger.LogDebug(Evt.FlowCookieSignOut, "Cleared flow cookie after missing external id.");
            return OAuthValidationError.FlowDataMissingOrInvalid;
        }

        var name = auth.Principal.FindFirst(ClaimTypes.Name)?.Value;
        var displayName = auth.Principal.FindFirst(OAuthConstants.ClaimDisplayName)?.Value;

        _logger.LogInformation(Evt.ValidateSuccess,
            "Validated OAuth flow. Provider={Provider}, Flow={Flow}, ExternalAccountHash={ExternalHash}, DisplayNamePresent={HasDisplayName}, NamePresent={HasName}. Took={ElapsedMs}ms",
            actualScheme, flow, HashForLogs(externalId), !string.IsNullOrEmpty(displayName), !string.IsNullOrEmpty(name), sw.ElapsedMilliseconds);

        sw.Stop();

        return new ValidatedFlowContext(
            Provider: actualScheme,
            Flow: flow,
            ExternalAccountId: externalId,
            ExternalAccountName: name ?? displayName,
            ExternalAccountDisplayName: displayName ?? name,
            Principal: auth.Principal,
            Properties: auth.Properties
        );
    }
}