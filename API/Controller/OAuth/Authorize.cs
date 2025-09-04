using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.OAuth;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/authorize")]
    public async Task<IActionResult> OAuthAuthorize([FromRoute] string provider, [FromServices] IAuthenticationSchemeProvider schemeProvider)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        return Challenge(new AuthenticationProperties { RedirectUri = $"/1/oauth/{provider}/handoff", Items = { { "flow", OAuthConstants.LoginOrCreate } } }, authenticationSchemes: [provider]);
    }
}