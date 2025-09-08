using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using System.Net.Mime;
using System.Security.Claims;
using OpenShock.API.OAuth;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Finalize an OAuth flow by either creating a new local account or linking to the current account.
    /// </summary>
    /// <remarks>
    /// Authenticates via the temporary OAuth flow cookie (set during the provider callback).
    /// - <b>create</b>: creates a local account, then links the external identity.<br/>
    /// - <b>link</b>: requires a logged-in local user; links the external identity to that user.<br/>
    /// No access/refresh tokens are returned.
    /// </remarks>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <param name="body">Finalize request.</param>
    /// <param name="accountService"></param>
    /// <param name="connectionService"></param>
    /// <response code="200">Finalization succeeded.</response>
    /// <response code="400">Flow not found, bad action, username invalid, or provider mismatch.</response>
    /// <response code="401">Link requested but user not authenticated.</response>
    /// <response code="409">External already linked to another account, or duplicate link attempt.</response>
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/finalize")]
    [ProducesResponseType(typeof(OAuthFinalizeResponse), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OpenShockProblem), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OpenShockProblem), StatusCodes.Status401Unauthorized, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OpenShockProblem), StatusCodes.Status409Conflict, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> OAuthFinalize(
        [FromRoute] string provider,
        [FromBody] OAuthFinalizeRequest body,
        [FromServices] IAccountService accountService,
        [FromServices] IOAuthConnectionService connectionService)
    {
        var result = await ValidateOAuthFlowAsync(provider);
        if (!result.TryPickT0(out var auth, out var response))
        {
            return response;
        }

        // External identity basics from claims (added by your handler)
        var externalId = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = body.Email ?? auth.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var displayName = body.Username ?? auth.Principal.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(displayName))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.FlowMissingData);
        }

        return body.Action switch
        {
            OAuthFlow.Link => await HandleLink(provider, externalId, displayName, connectionService),
            OAuthFlow.LoginOrCreate => await HandleLoginOrCreate(provider, externalId, email, displayName, accountService, connectionService),
            _ => await HandleBadFlow()
        };

        async Task<IActionResult> HandleBadFlow()
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.UnsupportedFlow);
        }
    }

    [NonAction]
    private async Task<IActionResult> HandleLink(string provider, string externalId, string displayName, IOAuthConnectionService connectionService)
    {
        // If the external is already linked, don’t allow relinking in either flow.
        var existing = await connectionService.GetByProviderExternalIdAsync(provider, externalId);
        if (existing is not null)
        {
            // Already linked to someone, block.
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.ExternalAlreadyLinked);
        }
        
        // Linking requires an authenticated session
        if (!TryGetAuthenticatedOpenShockUserId(out var currentUserId))
        {
            // Not a logged-in session (could be API token or anonymous)
            return Problem(OAuthError.NotAuthenticatedForLink);
        }


        var ok = await connectionService.TryAddConnectionAsync(
            userId: currentUserId,
            provider: provider,
            providerAccountId: externalId,
            providerAccountName: displayName
        );

        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

        if (!ok) return Problem(OAuthError.ExternalAlreadyLinked);

        return Ok(new OAuthFinalizeResponse
        {
            Provider = provider,
            ExternalId = externalId
        });
    }
    
    [NonAction]
    private async Task<IActionResult> HandleLoginOrCreate(string provider, string externalId, string email, string displayName, IAccountService accountService, IOAuthConnectionService connectionService)
    {
        // If the external is already linked, don’t allow relinking in either flow.
        var existing = await connectionService.GetByProviderExternalIdAsync(provider, externalId);
        if (existing is not null)
        {
            // External already mapped; treat as conflict (or you could return 200 if you consider this a no-op login).
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.ConnectionAlreadyExists);
        }

        // We must create a local account. Your AccountService requires a password, so:
        displayName = displayName.Trim();
        // TODO: Check if username valid, if invalid respond with bad request, dont clear cookie tho, so that frontend can try again

        var created = await accountService.CreateOAuthOnlyAccountAsync(
            email: email,
            username: displayName,
            provider: provider,
            providerAccountId: externalId,
            providerAccountName: displayName
        );


        if (created.IsT1)
        {
            return Problem(SignupError.UsernameOrEmailExists);
        }

        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);

        var newUser = created.AsT0.Value;

        return Ok(new OAuthFinalizeResponse
        {
            Provider = provider,
            ExternalId = externalId,
            Username = newUser.Name
        });
    }
}
