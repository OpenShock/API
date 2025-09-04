using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.OAuth;
using OpenShock.API.Extensions;
using OpenShock.API.OAuth;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    [HttpGet("connections/{provider}/link")]
    public async Task<IActionResult> AddOAuthConnection([FromRoute] string provider, [FromServices] IAuthenticationSchemeProvider schemeProvider)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        return Challenge(new AuthenticationProperties { RedirectUri = $"/1/oauth/{provider}/handoff", Items = {{ "flow", OAuthConstants.LinkFlow }} }, authenticationSchemes: [provider]);
    }
}