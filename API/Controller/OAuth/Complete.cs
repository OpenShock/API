using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.OAuth;
using OpenShock.API.OAuth.FlowStore;
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
    public async Task<IActionResult> OAuthComplete(
        [FromRoute] string provider,
        [FromServices] IAuthenticationSchemeProvider schemeProvider,
        [FromServices] IAccountService accountService,
        [FromServices] IOAuthFlowStore store)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        // Temp external principal (set by OAuth handler with SignInScheme=OAuthFlowScheme, SaveTokens=true)
        var auth = await HttpContext.AuthenticateAsync(OpenShockAuthSchemes.OAuthFlowScheme);
        if (!auth.Succeeded || auth.Principal is null)
            return BadRequest("OAuth sign-in not found or expired.");

        var props = auth.Properties;
        if (props is null || !props.Items.TryGetValue("flow", out var flow) || string.IsNullOrWhiteSpace(flow))
        {
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            Response.Cookies.Delete(OpenShockAuthSchemes.OAuthFlowCookie, new CookieOptions { Path = "/" });
            return BadRequest(new { error = "missing_flow" });
        }
        flow = flow.ToLowerInvariant();

        var ext = auth.Principal;
        var externalId =
            ext.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            ext.FindFirst("sub")?.Value ??
            ext.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Problem("Missing external subject.", statusCode: 400);

        var email = ext.FindFirst(ClaimTypes.Email)?.Value;
        var userName = ext.Identity?.Name;
        var tokens = (props.GetTokens() ?? Enumerable.Empty<AuthenticationToken>())
                     .ToDictionary(t => t.Name!, t => t.Value!);

        var connection = await accountService.GetOAuthConnectionAsync(provider, externalId);

        switch (flow)
        {
            case "login":
                {
                    if (connection is not null)
                    {
                        // Already linked -> sign in and go home.
                        // TODO: issue your UserSessionCookie/session here for connection.UserId
                        await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                        return Redirect("/");
                    }

                    var flowId = await SaveSnapshotAsync(store, provider, externalId, email, userName, tokens);
                    SetFlowCookie(flowId);
                    await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);

                    var frontend = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "https://app.example.com";
                    return Redirect($"{frontend}/{provider}/create");
                }

            case "link":
                {
                    if (connection is not null)
                    {
                        await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                        return Problem(
                            detail: "This external account is already linked to another user.",
                            statusCode: 409,
                            title: "Account already linked");
                    }

                    var flowId = await SaveSnapshotAsync(store, provider, externalId, email, userName, tokens);
                    SetFlowCookie(flowId);
                    await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);

                    var frontend = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "https://app.example.com";
                    return Redirect($"{frontend}/{provider}/link");
                }

            default:
                await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                Response.Cookies.Delete(OpenShockAuthSchemes.OAuthFlowCookie, new CookieOptions { Path = "/" });
                return BadRequest(new { error = "unknown_flow", flow });
        }

        // --- local helpers ---
        async Task<string> SaveSnapshotAsync(
            IOAuthFlowStore s, string prov, string extId, string? mail, string? name,
            IDictionary<string, string> tks)
        {
            var snapshot = new OAuthSnapshot(
                Provider: prov,
                ExternalId: extId,
                Email: mail,
                UserName: name,
                Tokens: tks,
                IssuedUtc: DateTimeOffset.UtcNow);
            return await s.SaveAsync(snapshot, OAuthFlow.Ttl);
        }

        void SetFlowCookie(string id)
        {
            Response.Cookies.Append(
                OpenShockAuthSchemes.OAuthFlowCookie,
                id,
                new CookieOptions
                {
                    Secure = HttpContext.Request.IsHttps,
                    HttpOnly = false,   // frontend reads once for /oauth/{provider}/data
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.Add(OAuthFlow.Ttl),
                    Path = "/"
                });
        }
    }

}