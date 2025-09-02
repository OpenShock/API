using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [EnableRateLimiting("auth")]
    [HttpGet("oauth/start", Name = "InternalSsoAuthenticate")]
    public async Task<IActionResult> OAuthAuthenticate([FromQuery] string provider, [FromServices] IAuthenticationSchemeProvider schemesProvider)
    {
        if (!await schemesProvider.IsSupportedOAuthProviderAsync(provider))
            return Problem(OAuthError.ProviderNotSupported);

        return Challenge(provider);
    }
}
