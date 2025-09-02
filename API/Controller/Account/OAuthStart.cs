using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.OAuth;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [EnableRateLimiting("auth")]
    [HttpGet("oauth/start")]
    public IActionResult OAuthAuthenticate([FromQuery] string provider, [FromQuery(Name = "return_to")] string? returnTo, [FromServices] IOAuthHandlerRegistry registry)
    {
        if (!registry.TryGet(provider, out var handler))
            return Problem(OAuthError.ProviderNotSupported);

        var result = handler.BuildAuthorizeUrl(HttpContext, new OAuthStartContext(string.IsNullOrWhiteSpace(returnTo) ? null : returnTo));
        return result.Match<IActionResult>(
            Redirect,
            error => Problem(title: error.Code, detail: error.Description)
        );
    }
}