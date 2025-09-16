using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.OAuth;
using OpenShock.Common.Authentication;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Start OAuth authorization for a given provider with the specified flow.
    /// </summary>
    /// <remarks>
    /// Returns <c>302</c> redirect to the provider authorization page.
    /// </remarks>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <param name="flow">Flow to run</param>
    /// <response code="302">Redirect to the provider authorization page.</response>
    /// <response code="400">Unsupported or misconfigured provider.</response>
    [EnableRateLimiting("auth")]
    [HttpPost("{provider}/authorize")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OAuthAuthorize([FromRoute] string provider, [FromQuery(Name="flow")] OAuthFlow flow)
    {
        if (!await _schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.UnsupportedProvider);
        
        var authenticate = await HttpContext.AuthenticateAsync(OpenShockAuthSchemes.UserSessionCookie);

        switch (flow)
        {
            case OAuthFlow.LoginOrCreate:
                if (authenticate.Succeeded) return Problem(OAuthError.FlowRequiresAnonymous);
                break;
            case OAuthFlow.Link:
                if (!authenticate.Succeeded) return Problem(OAuthError.FlowRequiresAuthenticatedUser);
                break;
            default:
                return Problem(OAuthError.UnsupportedFlow);
        }

        return OAuthUtil.StartOAuth(provider, flow);
    }
}