using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.OAuth;
using OpenShock.Common.Authentication;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Returns a list of supported SSO providers
    /// </summary>
    [HttpGet("oauth/providers")]
    public string[] ListOAuthProviders([FromServices] IOAuthHandlerRegistry registry)
    {
        return registry.ListProviders();
    }
}
