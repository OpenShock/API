using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.OAuth;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/authorize")]
    public IActionResult OAuthAuthorize([FromRoute] string provider, [FromQuery(Name = "return_to")] string? returnTo)
    {
        if (!_registry.TryGet(provider, out var handler))
            return Problem(OAuthError.ProviderNotSupported);

        var result = handler.BuildAuthorizeUrl(HttpContext, new OAuthStartContext(string.IsNullOrWhiteSpace(returnTo) ? null : returnTo));
        return result.Match<IActionResult>(
            Redirect,
            error => Problem(title: error.Code, detail: error.Description)
        );
    }
}