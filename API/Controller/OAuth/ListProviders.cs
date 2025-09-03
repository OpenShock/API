using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Extensions;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Returns a list of supported SSO provider keys
    /// </summary>
    [HttpGet("providers")]
    public async Task<string[]> ListOAuthProviders([FromServices] IAuthenticationSchemeProvider schemeProvider)
    {
        return await schemeProvider.GetAllOAuthSchemesAsync();
    }
}
