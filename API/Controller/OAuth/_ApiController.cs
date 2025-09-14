using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OpenShock.API.OAuth;
using OpenShock.API.Services.Account;
using OpenShock.Common;

namespace OpenShock.API.Controller.OAuth;

/// <summary>
/// OAuth management endpoints (provider listing, authorize, data handoff).
/// </summary>
[ApiController]
[Tags("OAuth")]
[ApiVersion("1")]
[Route("/{version:apiVersion}/oauth")]
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
        // 1) authenticate temp cookie
        var auth = await HttpContext.AuthenticateAsync(OAuthConstants.FlowScheme);
        if (!auth.Succeeded || auth.Principal is null || auth.Ticket is null)
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return OAuthValidationError.FlowStateMissing;
        }

        // 2) scheme/provider check - prefer the ticket's scheme over a magic Item
        if (!auth.Properties.Items.TryGetValue(".AuthScheme", out var actualScheme) || string.IsNullOrWhiteSpace(actualScheme))
        {
            _logger.LogError("Invalid OAuth scheme");
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return OAuthValidationError.FlowDataMissingOrInvalid;
        }

        // 3) parse flow from properties
        if (auth.Properties is null ||
            !auth.Properties.Items.TryGetValue(OAuthConstants.ItemKeyFlowType, out var flowStr) ||
            !Enum.TryParse<OAuthFlow>(flowStr, true, out var flow))
        {
            _logger.LogError("Invalid OAuth scheme");
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return OAuthValidationError.FlowDataMissingOrInvalid;
        }
        
        // 4) fetch id of external user
        var externalId = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(externalId))
        {
            _logger.LogError("Invalid OAuth scheme");
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return OAuthValidationError.FlowDataMissingOrInvalid;
        }
        
        return new ValidatedFlowContext(
            Provider: actualScheme,
            Flow: flow,
            ExternalAccountId: externalId,
            ExternalAccountName: auth.Principal.FindFirst(OAuthConstants.ClaimUserName)?.Value,
            ExternalAccountDisplayName: auth.Principal.FindFirst(OAuthConstants.ClaimDisplayName)?.Value,
            Principal: auth.Principal,
            Properties: auth.Properties
        );
    }
}