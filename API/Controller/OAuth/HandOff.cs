using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.OAuth.FlowStore;
using OpenShock.API.Services.Account;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenShock.API.OAuth;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/handoff")]
    public async Task<IActionResult> OAuthHandOff(
        [FromRoute] string provider,
        [FromServices] IAuthenticationSchemeProvider schemeProvider,
        [FromServices] IAccountService accountService)
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
            case OAuthConstants.LoginOrCreate:
                {
                    if (connection is not null)
                    {
                        // Already linked -> sign in and go home.
                        // TODO: issue your UserSessionCookie/session here for connection.UserId
                        await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                        return Redirect("/");
                    }

                    var frontend = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "https://app.example.com";
                    return Redirect($"{frontend}/{provider}/create");
                }

            case OAuthConstants.LinkFlow:
                {
                    if (connection is not null)
                    {
                        // TODO: Check if the connection is connected to our account with same externalId (AlreadyLinked), different externalId (AlreadyExists), or to another account (LinkedToAnotherAccount)
                        await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                        return Problem(OAuthError.LinkedToAnotherAccount);
                    }

                    var frontend = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "https://app.example.com";
                    return Redirect($"{frontend}/{provider}/link");
                }

            default:
                await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                return Problem(OAuthError.FlowNotSupported);
        }
    }
}