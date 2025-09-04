using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.OAuth;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Get the list of supported OAuth providers.
    /// </summary>
    /// <remarks>
    /// Returns the set of provider keys that are configured and available for use.
    /// </remarks>
    /// <response code="200">Returns provider keys (e.g., <c>discord</c>).</response>
    [HttpGet("providers")]
    public async Task<string[]> ListOAuthProviders()
    {
        return await _schemeProvider.GetAllOAuthSchemesAsync();
    }
}
