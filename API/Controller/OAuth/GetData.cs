using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.OAuth;
using OpenShock.API.OAuth.FlowStore;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [ResponseCache(NoStore = true)]
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/data")]
    public async Task<IActionResult> OAuthGetData(
        [FromRoute] string provider,
        [FromServices] IAuthenticationSchemeProvider schemeProvider,
        [FromServices] IOAuthFlowStore store)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        // Temp external principal (set by OAuth handler with SignInScheme=OAuthFlowScheme, SaveTokens=true)
        var auth = await HttpContext.AuthenticateAsync(OpenShockAuthSchemes.OAuthFlowScheme);
        if (!auth.Succeeded || auth.Principal is null)
            return Problem(OAuthError.FlowNotFound);

        // Read identifiers from claims (no props.Items)
        var flowIdClaim = auth.Principal.FindFirst("flow_id")?.Value;
        var providerClaim = auth.Principal.FindFirst("provider")?.Value;

        if (string.IsNullOrWhiteSpace(flowIdClaim) || string.IsNullOrWhiteSpace(providerClaim))
        {
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Problem(OAuthError.FlowNotFound);
        }

        // Load snapshot
        var snap = await store.GetAsync(flowIdClaim);
        if (snap is null)
        {
            // Stale/missing -> clear temp scheme (cookie+store entry) to stop loops
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Problem(OAuthError.FlowNotFound);
        }

        // Defensive: ensure the snapshot belongs to this provider
        if (snap.Provider != provider)
        {
            // Optional: you may also delete the cookie if you consider this a poisoned flow
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Problem(OAuthError.ProviderMismatch);
        }

        var dto = new OAuthPublic
        {
            Provider = snap.Provider,
            Email = snap.Email,
            DisplayName = snap.DisplayName,
            ExpiresAt = snap.IssuedUtc.Add(OAuthConstants.StateLifetime).UtcDateTime
        };

        return Ok(dto);
    }
}
