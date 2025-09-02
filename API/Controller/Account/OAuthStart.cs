using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [EnableRateLimiting("auth")]
    [HttpGet("oauth/start", Name = "InternalSsoAuthenticate")]
    public async Task<IActionResult> OAuthAuthenticate([FromQuery] string provider, [FromQuery] Uri? redirectUrl)
    {
        if (!OpenShockAuthSchemes.OAuth2Schemes.Contains(provider))
            return Problem(OAuthError.ProviderNotSupported);

        // TODO: Generate the provider's OAuth URL
    }
}
