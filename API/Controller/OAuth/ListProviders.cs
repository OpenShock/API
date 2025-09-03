using Microsoft.AspNetCore.Mvc;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Returns a list of supported SSO provider keys
    /// </summary>
    [HttpGet("providers")]
    public string[] ListOAuthProviders()
    {
        return _registry.ListProviderKeys();
    }
}
