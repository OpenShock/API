using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.OAuth;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    [HttpPost("connections/{provider}/authorize")]
    public async Task<IActionResult> AddOAuthConnection([FromRoute] string provider, [FromQuery(Name = "return_to")] string returnTo, [FromServices] IOAuthHandlerRegistry registry)
    {
        if (await _accountService.HasOAuthConnectionAsync(CurrentUser.Id, provider))
        {
            return Problem(OAuthError.AlreadyExists);
        }

        var result = await registry.StartAuthorizeAsync(HttpContext, provider, OAuthFlow.Link, returnTo);
        return result.Match<IActionResult>(
            uri => Redirect(uri.ToString()),
            error => Problem(title: error.Code, detail: error.Description),
            notSupported => Problem(OAuthError.ProviderNotSupported)
        );
    }
}