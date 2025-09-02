using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [HttpGet("oauth/callback/{provider}")]
    [EnableCors("allow_sso_providers")]
    public async Task<IActionResult> OAuthAuthenticate([FromRoute] string provider, [FromQuery] string code, [FromServices] IAuthenticationSchemeProvider schemesProvider)
    {
        if (!await schemesProvider.IsSupportedOAuthProviderAsync(provider))
            return Problem(OAuthError.ProviderNotSupported);

        return Challenge(provider);
    }
}
