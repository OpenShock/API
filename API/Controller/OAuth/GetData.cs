using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.OAuth;
using OpenShock.API.OAuth.FlowStore;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using Scalar.AspNetCore;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/data")]
    public async Task<IActionResult> OAuthGetData(
        [FromRoute] string provider,
        [FromServices] IAuthenticationSchemeProvider schemeProvider,
        [FromServices] IOAuthFlowStore store)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        if (!Request.Cookies.TryGetValue(OpenShockAuthSchemes.OAuthFlowCookie, out var flowId) ||
            string.IsNullOrWhiteSpace(flowId))
            return NotFound(new { error = "no_flow" });

        var snap = await store.GetAsync(flowId);

        if (snap is null)
        {
            // Clean up stale cookie to avoid client polling loops
            Response.Cookies.Delete(OpenShockAuthSchemes.OAuthFlowCookie, new CookieOptions { Path = "/" });
            return NotFound(new { error = "expired" });
        }

        // Defensive: ensure the snapshot belongs to this provider
        if (!string.Equals(snap.Provider, provider, StringComparison.OrdinalIgnoreCase))
        {
            // Optional: you may also delete the cookie if you consider this a poisoned flow
            Response.Cookies.Delete(OpenShockAuthSchemes.OAuthFlowCookie, new CookieOptions { Path = "/" });
            // Prefer NotFound to avoid leaking existence across providers
            return NotFound(new { error = "provider_mismatch" });
            // Or: return Conflict(new { error = "provider_mismatch" });
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = snap.IssuedUtc.Add(OAuthFlow.Ttl);
        var expiresIn = (int)Math.Max(0, (expiresAt - now).TotalSeconds);

        var dto = new OAuthPublic(
            provider: snap.Provider,
            externalId: snap.ExternalId,
            email: snap.Email,
            userName: snap.UserName,
            flowId: flowId,
            expiresInSeconds: expiresIn
        );

        // Don’t let proxies/browsers cache this
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";

        return Ok(dto);
    }
}
