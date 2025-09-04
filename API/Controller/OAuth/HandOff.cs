using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenShock.API.Extensions;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Authentication;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using System.Net.Mime;
using System.Security.Claims;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Handoff after provider callback. Decides next step (create, link, or direct sign-in).
    /// </summary>
    /// <remarks>
    /// This endpoint is reached after the OAuth middleware processed the provider callback.  
    /// It reads the temp OAuth flow principal and its <c>flow</c> (create/link).  
    /// If an existing connection is found, signs in and redirects home; otherwise redirects the frontend to continue the flow.
    /// </remarks>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <param name="accountService"></param>
    /// <param name="connectionService"></param>
    /// <param name="frontendOptions"></param>
    /// <response code="302">Redirect to the frontend (create/link) or home on direct sign-in.</response>
    /// <response code="400">Flow missing or not supported.</response>
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/handoff")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> OAuthHandOff(
        [FromRoute] string provider,
        [FromServices] IAccountService accountService,
        [FromServices] IOAuthConnectionService connectionService,
        [FromServices] IOptions<FrontendOptions> frontendOptions)
    {
        if (!await _schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.UnsupportedProvider);

        // Temp external principal (set by OAuth SignInScheme).
        var auth = await HttpContext.AuthenticateAsync(OpenShockAuthSchemes.OAuthFlowScheme);
        if (!auth.Succeeded || auth.Principal is null)
            return Problem(OAuthError.FlowNotFound);

        // Flow is stored in AuthenticationProperties by the authorize step.
        if (auth.Properties is null || !auth.Properties.Items.TryGetValue("flow", out var flow) || string.IsNullOrWhiteSpace(flow))
        {
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Problem(OAuthError.InternalError);
        }
        flow = flow.ToLowerInvariant();

        // External subject is required to resolve/link.
        var externalId = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
        {
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Problem(OAuthError.FlowMissingData);
        }

        var connection = await connectionService.GetByProviderExternalIdAsync(provider, externalId);

        switch (flow)
        {
            case AuthConstants.OAuthLoginOrCreateFlow:
                {
                    if (connection is not null)
                    {
                        // Already linked -> sign in and go home.
                        // TODO: issue your UserSessionCookie/session here for connection.UserId
                        await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                        return Redirect("/");
                    }
                    
                    var frontendUrl = new UriBuilder(frontendOptions.Value.BaseUrl)
                    {
                        Path = $"oauth/{provider}/create"
                    };
                    return Redirect(frontendUrl.Uri.ToString());
                }

            case AuthConstants.OAuthLinkFlow:
                {
                    if (connection is not null)
                    {
                        // TODO: Check if the connection is connected to our account with same externalId (AlreadyLinked), different externalId (AlreadyExists), or to another account (LinkedToAnotherAccount)
                        await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                        return Problem(OAuthError.ExternalAlreadyLinked);
                    }

                    var frontendUrl = new UriBuilder(frontendOptions.Value.BaseUrl)
                    {
                        Path = $"oauth/{provider}/link"
                    };
                    return Redirect(frontendUrl.Uri.ToString());
                }

            default:
                await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                return Problem(OAuthError.UnsupportedFlow);
        }
    }
}