using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Extensions;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    [HttpGet("connections/{provider}/link")]
    public async Task<IActionResult> AddOAuthConnection([FromRoute] string provider, [FromServices] IAuthenticationSchemeProvider schemeProvider)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        return Challenge(new AuthenticationProperties { RedirectUri = $"/oauth/{provider}/complete", Parameters = {{ "flow", "link" }} }, authenticationSchemes: [provider]);
    }
}