using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.OAuth;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    [HttpGet("connections/{provider}")]
    public async Task<IActionResult> AddOAuthConnection([FromRoute] string provider, [FromQuery(Name = "return_to")] string? returnTo, [FromServices] IOAuthHandlerRegistry registry)
    {
        if (!registry.TryGet(provider, out var handler))
            return Problem(OAuthError.ProviderNotSupported);

        if (await _accountService.HasOAuthConnectionAsync(CurrentUser.Id, provider))
        {
            return Problem(OAuthError.AlreadyExists);
        }

        var result = handler.BuildAuthorizeUrl(HttpContext, new OAuthStartContext(string.IsNullOrWhiteSpace(returnTo) ? null : returnTo));
        return result.Match<IActionResult>(
            Redirect,
            error => Problem(title: error.Code, detail: error.Description)
        );
    }
}