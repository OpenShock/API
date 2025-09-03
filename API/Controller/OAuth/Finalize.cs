using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.OAuth;
using OpenShock.API.OAuth.FlowStore;
using OpenShock.API.Services.Account;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/finalize")]
    public async Task<IActionResult> OAuthFinalize(
        [FromRoute] string provider,
        [FromBody] OAuthFinalizeRequest body,
        [FromServices] IAuthenticationSchemeProvider schemeProvider,
        [FromServices] IOAuthFlowStore store,
        [FromServices] IAccountService accountService,
        [FromServices] IUserReferenceService userReferenceService,
        [FromServices] IClientAuthService<User> clientAuthService // used to read current user (if any)
    )
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        if (!ModelState.IsValid)
            return BadRequest(new { error = "bad_request", details = ModelState });

        var action = body.action?.Trim().ToLowerInvariant();
        if (action is not ("create" or "link"))
            return BadRequest(new { error = "unknown_action" });

        // Load snapshot (one-time handoff)
        var snapshot = await store.GetAsync(body.flowId);
        if (snapshot is null)
        {
            // stale/expired/consumed flow; clear client cookie too
            Response.Cookies.Delete(OpenShockAuthSchemes.OAuthFlowCookie, new CookieOptions { Path = "/" });
            return BadRequest(new { error = "expired" });
        }

        // Provider must match the route (defense-in-depth)
        if (!string.Equals(snapshot.Provider, provider, StringComparison.OrdinalIgnoreCase))
        {
            await store.DeleteAsync(body.flowId);
            Response.Cookies.Delete(OpenShockAuthSchemes.OAuthFlowCookie, new CookieOptions { Path = "/" });
            return Conflict(new { error = "provider_mismatch" });
        }

        // From here on, ensure we always clean up the flow
        await store.DeleteAsync(body.flowId);
        Response.Cookies.Delete(OpenShockAuthSchemes.OAuthFlowCookie, new CookieOptions { Path = "/" });

        // Does this external already exist?
        var existing = await accountService.GetOAuthConnectionAsync(provider, snapshot.ExternalId);

        if (action == "create")
        {
            string userId;

            if (existing is not null)
            {
                // Already linked → log that user in
                userId = existing.UserId;
            }
            else
            {
                // Create a new local user, then link the external
                // TODO: replace with your actual APIs to create a user and link OAuth
                // Examples (rename to your signatures):
                userId = await accountService.CreateUserAsync(
                    preferredUserName: snapshot.UserName ?? $"oauth_{provider}_{snapshot.ExternalId}",
                    email: snapshot.Email);

                await accountService.AddOAuthConnectionAsync(
                    userId: userId,
                    provider: provider,
                    externalId: snapshot.ExternalId,
                    tokens: snapshot.Tokens);
            }

            // Issue your application session now
            // TODO: replace token issuance with your real session creation + cookie write
            var ctx = new LoginContext
            {
                Ip = HttpContext.GetRemoteIP().ToString(),
                UserAgent = HttpContext.GetUserAgent(),
            };

            var loginAction = await accountService.CreateUserLoginSessionAsync(userId, ctx, HttpContext.RequestAborted);
            return await loginAction.MatchAsync<IActionResult>(
                ok: async ok =>
                {
                    // Choose your cookie domain policy as needed
                    HttpContext.SetSessionKeyCookie(ok.Token /*, "." + cookieDomainToUse */);
                    // Ensure the external temp principal is gone (if any)
                    await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
                    return Ok(new { status = "ok" });
                },
                deactivated: _ => Task.FromResult<IActionResult>(Problem(AccountError.AccountDeactivated)),
                oauthOnly: _ => Task.FromResult<IActionResult>(Problem(AccountError.AccountOAuthOnly)),
                notActivated: _ => Task.FromResult<IActionResult>(Problem(AccountError.AccountNotActivated)),
                notFound: _ => Task.FromResult<IActionResult>(Problem(LoginError.InvalidCredentials))
            );
        }

        // action == "link"
        // Caller must already be authenticated with your site
        if (!(userReferenceService.AuthReference?.Value.IsT0 ?? false))
            return Unauthorized(new { error = "not_authenticated" });

        var currentUser = clientAuthService.CurrentClient;
        if (currentUser is null)
            return Unauthorized(new { error = "not_authenticated" });

        if (existing is not null)
        {
            // Someone already owns this external identity
            return Conflict(new { error = "already_linked" });
        }

        // Attach the external to the current user
        await accountService.AddOAuthConnectionAsync(
            userId: currentUser.Id,
            provider: provider,
            externalId: snapshot.ExternalId,
            tokens: snapshot.Tokens);

        // Optional: refresh/extend current session if you need to
        // await clientAuthService.RefreshAsync(...);

        // Ensure the external temp principal is gone (if any)
        await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);

        return Ok(new { status = "ok" });
    }
}
