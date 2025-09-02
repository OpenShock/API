using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [EnableRateLimiting("auth")]
    [HttpGet("oauth/callback/{provider}")]
    [EnableCors("allow_sso_providers")]
    public async Task<IActionResult> OAuthAuthenticate([FromRoute] string provider)
    {
        if (!OpenShockAuthSchemes.OAuth2Schemes.Contains(provider))
            return Problem(OAuthError.ProviderNotSupported);
        
        // TODO: Validate OAuth response and fetch user details to create/authenticate account
    }
}
