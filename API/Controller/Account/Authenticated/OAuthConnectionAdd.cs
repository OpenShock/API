using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.OAuth;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    [HttpPost("connections/{provider}/authorize")]
    public async Task<IActionResult> AddOAuthConnection([FromRoute] string provider, [FromQuery(Name = "return_to")] string? returnTo, [FromServices] IOAuthHandlerRegistry registry)
    {
        if (!registry.TryGet(provider, out var handler))
            return Problem(OAuthError.ProviderNotSupported);

        if (await _accountService.HasOAuthConnectionAsync(CurrentUser.Id, provider))
        {
            return Problem(OAuthError.AlreadyExists);
        }

        // Private authorize endpoint => Link flow
        var ctx = new OAuthStartContext(
            ReturnTo: string.IsNullOrWhiteSpace(returnTo) ? null : returnTo,
            Flow: OAuthFlow.Link
        );

        var result = await handler.BuildAuthorizeUrlAsync(HttpContext, ctx);
        return result.Match<IActionResult>(
            Redirect,
            error => Problem(title: error.Code, detail: error.Description)
        );
    }
}