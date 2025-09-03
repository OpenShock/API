using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.OAuth;
using OpenShock.Common.Errors;
using System.Threading.Tasks;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/authorize")]
    public async Task<IActionResult> OAuthAuthorize([FromRoute] string provider, [FromQuery(Name = "return_to")] string? returnTo)
    {
        if (!_registry.TryGet(provider, out var handler))
            return Problem(OAuthError.ProviderNotSupported);

        // Public authorize endpoint => SignIn flow
        var ctx = new OAuthStartContext(
            ReturnTo: string.IsNullOrWhiteSpace(returnTo) ? null : returnTo,
            Flow: OAuthFlow.SignIn
        );

        var result = await handler.BuildAuthorizeUrlAsync(HttpContext, ctx);
        return result.Match<IActionResult>(
            Redirect,
            error => Problem(title: error.Code, detail: error.Description)
        );
    }
}