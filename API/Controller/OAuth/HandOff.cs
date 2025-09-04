using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.Services.Account;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using OpenShock.Common.Constants;
using OpenShock.Common.Options;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/handoff")]
    public async Task<IActionResult> OAuthHandOff(
        [FromRoute] string provider,
        [FromServices] IAuthenticationSchemeProvider schemeProvider,
        [FromServices] IAccountService accountService,
        [FromServices] IOptions<FrontendOptions> frontendOptions)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        // Temp external principal (set by OAuth handler with SignInScheme=OAuthFlowScheme, SaveTokens=true)
        var auth = await HttpContext.AuthenticateAsync(OpenShockAuthSchemes.OAuthFlowScheme);
        if (!auth.Succeeded || auth.Principal is null)
            return Problem(OAuthError.FlowNotFound);

        if (auth.Properties is null || !auth.Properties.Items.TryGetValue("flow", out var flow) || string.IsNullOrWhiteSpace(flow))
        {
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Problem(OAuthError.InternalError);
        }
        flow = flow.ToLowerInvariant();

        var externalId = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
        {
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Problem(OAuthError.FlowMissingData);
        }

        var connection = await accountService.GetOAuthConnectionAsync(provider, externalId);

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
                        return Problem(OAuthError.LinkedToAnotherAccount);
                    }

                    var frontendUrl = new UriBuilder(frontendOptions.Value.BaseUrl)
                    {
                        Path = $"oauth/{provider}/link"
                    };
                    return Redirect(frontendUrl.Uri.ToString());
                }

            default:
                await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                return Problem(OAuthError.FlowNotSupported);
        }
    }
}