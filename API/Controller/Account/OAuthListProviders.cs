using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Authentication;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Returns a list of supported SSO providers
    /// </summary>
    [HttpGet("oauth/providers")]
    public string[] ListOAuthProviders()
    {
        return OpenShockAuthSchemes.OAuth2Schemes;
    }
}
