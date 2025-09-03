using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/authorize")]
    public async Task<IActionResult> OAuthAuthorize([FromRoute] string provider, [FromQuery(Name = "return_to")] string returnTo, [FromServices] IAuthenticationSchemeProvider schemeProvider)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        return Challenge(new AuthenticationProperties { RedirectUri = $"/oauth/{provider}/complete", Items = { { "flow", "login" } } }, authenticationSchemes: [provider]);
    }
}