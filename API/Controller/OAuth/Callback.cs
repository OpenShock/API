using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> OAuthCallback([FromRoute] string provider)
    {
        if (!_registry.TryGet(provider, out var handler))
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