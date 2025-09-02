using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Extensions;
using System.Net.Mime;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Returns a list of supported SSO providers
    /// </summary>
    [HttpGet("oauth/providers", Name = "GetOAuthProviderlist")]
    [EnableRateLimiting("auth")]
    public async Task<string[]> ListOAuthProviders([FromServices] IAuthenticationSchemeProvider schemesProvider)
    {
        return await schemesProvider.GetOAuthSchemeNamesAsync();
    }
}
