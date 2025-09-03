using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.OAuth;
using OpenShock.API.Services.Account;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using Scalar.AspNetCore;
using System.Security.Claims;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/complete")]
    public async Task<IActionResult> OAuthComplete([FromRoute] string provider, [FromServices] IAuthenticationSchemeProvider schemeProvider, [FromServices] IAccountService accountService)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        // External principal placed by the OAuth handler (SaveTokens=true, SignInScheme=OAuthFlowScheme)
        var auth = await HttpContext.AuthenticateAsync(OpenShockAuthSchemes.OAuthFlowScheme);
        if (!auth.Succeeded || auth.Principal is null)
            return BadRequest("OAuth sign-in not found or expired.");

        var ext = auth.Principal;
        var props = auth.Properties;

        // Essentials from external identity
        var externalId = ext.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? ext.FindFirst("sub")?.Value
                      ?? ext.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(externalId))
            return Problem("Missing external subject.", statusCode: 400);

        var email = ext.FindFirst(ClaimTypes.Email)?.Value;
        var userName = ext.Identity?.Name;

        var tokens = (props?.GetTokens() ?? Enumerable.Empty<AuthenticationToken>())
                     .ToDictionary(t => t.Name!, t => t.Value!);

        // Who (if anyone) is currently signed into OUR site?
        var currentUserId = HttpContext.User?.FindFirst("uid")?.Value;

        // Is this external already linked to someone?
        var connection = await accountService.GetOAuthConnectionAsync(provider, externalId);

        // CASE A: External already linked
        if (connection is not null)
        {
            if (!string.IsNullOrEmpty(currentUserId))
            {
                // Already logged in locally.
                if (connection.UserId == currentUserId)
                {
                    // Happy path: ensure session is fresh and go home.
                    await sessionIssuer.SignInAsync(HttpContext, connection.UserId);
                    await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                    return Redirect("/");
                }

                // Linked to a different local account → fail explicitly.
                await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                return Problem(
                    detail: "This external account is already linked to another user.",
                    statusCode: 409,
                    title: "Account already linked");
            }

            // Anonymous user: sign in as the linked account and go home.
            await sessionIssuer.SignInAsync(HttpContext, connection.UserId);
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Redirect("/");
        }

        // CASE B: Not linked yet → create flow snapshot and send to frontend for link/create
        var snapshot = new OAuthSnapshot(
            Provider: provider,
            ExternalId: externalId,
            Email: email,
            UserName: userName,
            Tokens: tokens,
            IssuedUtc: DateTimeOffset.UtcNow);

        var flowId = await store.SaveAsync(snapshot, OAuthFlow.Ttl);

        // Short-lived, non-HttpOnly cookie so the frontend can call /oauth/{provider}/data
        Response.Cookies.Append(
            OAuthFlow.TempCookie,
            flowId,
            new CookieOptions
            {
                Secure = HttpContext.Request.IsHttps,
                HttpOnly = false,            // readable by frontend JS for one fetch
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.Add(OAuthFlow.Ttl),
                Path = "/"
            });

        // Clean up the temp external principal
        await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);

        // Decide which UI route to send them to
        var frontend = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "https://app.example.com";
        var nextPath = (!string.IsNullOrEmpty(currentUserId))
            ? $"/{provider}/link"
            : $"/{provider}/create";

        return Redirect(frontend + nextPath);
    }
}