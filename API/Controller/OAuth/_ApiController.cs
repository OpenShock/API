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

    /// <summary>
    /// Validates: provider exists, temp cookie auth present, scheme matches, flow parsable.
    /// On success returns ValidatedFlowContext; on failure returns IActionResult with proper problem details.
    /// </summary>
    private async Task<OneOf<ValidatedFlowContext, IActionResult>> ValidateOAuthFlowAsync(string expectedProvider, OAuthFlow? expectedFlow = null)
    {
        // 1) provider supported?
        if (!await _schemeProvider.IsSupportedOAuthScheme(expectedProvider))
            return Problem(OAuthError.UnsupportedProvider);

        // 2) authenticate temp cookie
        var auth = await HttpContext.AuthenticateAsync(OAuthConstants.FlowScheme);
        if (!auth.Succeeded || auth.Principal is null || auth.Ticket is null)
            return Problem(OAuthError.FlowStateNotFound);

        // 3) scheme/provider check — prefer the ticket's scheme over a magic Item
        if (!auth.Properties.Items.TryGetValue(".AuthScheme", out var actualScheme) || string.IsNullOrEmpty(actualScheme))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.FlowStateNotFound);
        }
        
        if (actualScheme != expectedProvider)
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.ProviderMismatch);
        }

        // 4) parse flow from properties
        if (auth.Properties is null ||
            !auth.Properties.Items.TryGetValue(OAuthConstants.ItemKeyFlowType, out var flowStr) ||
            !Enum.TryParse<OAuthFlow>(flowStr, true, out var flow))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.UnsupportedFlow);
        }

        if (expectedFlow is not null && flow != expectedFlow)
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.FlowMismatch);
        }
        
        // External subject is required to resolve/link.
        var externalId = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.FlowMissingData);
        }

        return new ValidatedFlowContext(
            Provider: expectedProvider,
            Flow: flow,
            ExternalAccountId: externalId,
            Principal: auth.Principal,
            Properties: auth.Properties
        );
    }
}