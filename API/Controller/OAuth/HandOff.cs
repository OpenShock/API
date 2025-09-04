using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using System.Net.Mime;
using System.Security.Claims;
using OpenShock.API.OAuth;

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
        [FromServices] IOAuthConnectionService connectionService,
        [FromServices] IOptions<FrontendOptions> frontendOptions)
    {
        var result = await ValidateOAuthFlowAsync(provider);
        if (!result.TryPickT0(out var auth, out var response))
        {
            return response;
        }
        
        // External subject is required to resolve/link.
        var externalId = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
        {
            await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
            return Problem(OAuthError.FlowMissingData);
        }

        var connection = await connectionService.GetByProviderExternalIdAsync(provider, externalId);

        switch (auth.Flow)
        {
            case OAuthFlow.LoginOrCreate:
                {
                    // TODO: Fail if currently logged in
                    
                    if (connection is not null)
                    {
                        // Log In
                        // TODO: Initialize authentication session
                        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                        return Redirect("/");
                    }
                    
                    // Create
                    
                    var frontendUrl = new UriBuilder(frontendOptions.Value.BaseUrl)
                    {
                        Path = $"oauth/{provider}/create"
                    };
                    return Redirect(frontendUrl.Uri.ToString());
                }

            case OAuthFlow.Link:
                {
                    if (connection is not null)
                    {
                        // Connection already exists, FAILURE
                        
                        // TODO: Check if the connection is connected to our account with same externalId (AlreadyLinked), different externalId (AlreadyExists), or to another account (LinkedToAnotherAccount)
                        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                        return Problem(OAuthError.ExternalAlreadyLinked);
                    }
                    
                    // Link connection to account

                    var frontendUrl = new UriBuilder(frontendOptions.Value.BaseUrl)
                    {
                        Path = $"oauth/{provider}/link"
                    };
                    return Redirect(frontendUrl.Uri.ToString());
                }

            default:
                await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                return Problem(OAuthError.UnsupportedFlow);
        }
    }
}