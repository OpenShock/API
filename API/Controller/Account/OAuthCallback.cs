using System.Diagnostics;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.OAuth;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [EnableRateLimiting("auth")]
    [HttpGet("oauth/callback/{provider}")]
    [EnableCors("allow_sso_providers")]
    public async Task<IActionResult> OAuthAuthenticate([FromRoute] string provider, [FromServices] IOAuthHandlerRegistry registry)
    {
        if (!registry.TryGet(provider, out var handler))
            return Problem(OAuthError.ProviderNotSupported);

        // Let the handler do everything (state validation, token exchange, user fetch)
        var result = await handler.HandleCallbackAsync(HttpContext, Request.Query);
        if (!result.TryPickT0(out var contract, out var error))
        {
            return BadRequest(); // TODO: Change me
        }

        // >>> Your app-specific login/linking <<<
        // e.g., sign in / create session by result.User

        // Decide where to go next (consider a per-provider default or read from state store if you saved return_to)
        return Redirect("https://app.openshock.app/auth/callback/" + handler.Key); // or your chosen target
    }
}