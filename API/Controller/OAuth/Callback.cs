using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> OAuthCallback([FromRoute] string provider)
    {
        // Let the handler do everything (state validation, token exchange, user fetch)
        var result = await _registry.HandleCallbackAsync(HttpContext, provider, Request.Query);
        return result.Match<IActionResult>(
            ok =>
            {
                // >>> Your app-specific login/linking <<<
                // e.g., sign in / create session by result.User

                return Redirect(ok.CallbackUrl);
            },
            error => BadRequest(), // TODO: Change me
            notSupported => Problem(OAuthError.ProviderNotSupported)
        );
    }
}