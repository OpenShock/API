using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using OpenShock.API.OAuth;
using OpenShock.Common.Errors;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

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
    /// <param name="sessionService"></param>
    /// <param name="frontendOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="302">Redirect to the frontend (create/link) or home on direct sign-in.</response>
    /// <response code="400">Flow missing or not supported.</response>
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/handoff")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> OAuthHandOff(
        [FromRoute] string provider,
        [FromServices] IOAuthConnectionService connectionService,
        [FromServices] ISessionService sessionService, 
        [FromServices] IOptions<FrontendOptions> frontendOptions,
        CancellationToken cancellationToken)
    {
        var result = await ValidateOAuthFlowAsync(provider);
        if (!result.TryPickT0(out var auth, out var response))
        {
            return response;
        }

        var connection = await connectionService.GetByProviderExternalIdAsync(provider, auth.ExternalAccountId);

        switch (auth.Flow)
        {
            case OAuthFlow.LoginOrCreate:
                {
                    if (IsOpenShockUserAuthenticated())
                    {
                        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                        return GetBadUrl("mustBeAuthenticated");
                    }
                    
                    if (connection is not null)
                    {
                        var cookieDomainToUse = frontendOptions.Value.CookieDomain.Split(',').FirstOrDefault(domain => Request.Headers.Host.ToString().EndsWith(domain, StringComparison.OrdinalIgnoreCase));
                        if (cookieDomainToUse is null) return Problem(LoginError.InvalidDomain);
                        
                        var session = await sessionService.CreateSessionAsync(
                            connection.UserId,
                            HttpContext.GetUserAgent(),
                            HttpContext.GetRemoteIP().ToString()
                            );
                        
                        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                        HttpContext.SetSessionKeyCookie(session.Token, "." + cookieDomainToUse);
                        
                        return Redirect("/"); // TODO: Make this go to frontend
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
                    if (!IsOpenShockUserAuthenticated())
                    {
                        await HttpContext.SignOutAsync(OAuthConstants.FlowScheme);
                        return GetBadUrl("cannotBeAuthenticated");
                    }
                    
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

        RedirectResult GetBadUrl(string errorType)
        {
            var frontendUrl = new UriBuilder(frontendOptions.Value.BaseUrl)
            {
                Path = "some/bad/url",
                Query = new QueryBuilder
                {
                    { "error", errorType }
                }.ToString()
            };
            return Redirect(frontendUrl.Uri.ToString());
        }
    }
}