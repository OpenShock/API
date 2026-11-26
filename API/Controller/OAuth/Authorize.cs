using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.OAuth;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Extensions;

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
    /// <param name="activationToken">Token from the activation email; signing in with the account's linked provider then activates it.</param>
    /// <response code="302">Redirect to the provider authorization page.</response>
    /// <response code="400">Unsupported or misconfigured provider.</response>
    [HttpPost("{provider}/authorize")]
    [ApiVersion(1)]
    [EnableRateLimiting("auth")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OAuthAuthorize([FromRoute] string provider, [FromQuery(Name="flow"), Required] OAuthFlow flow, [FromQuery(Name = "activationToken"), StringLength(128)] string? activationToken = null)
    {
        if (!await _schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.UnsupportedProvider);

        switch (flow)
        {
            case OAuthFlow.LoginOrCreate:
                if (User.HasOpenShockUserIdentity()) return Problem(OAuthError.FlowRequiresAnonymous);
                break;
            case OAuthFlow.Link:
                if (!User.HasOpenShockUserIdentity()) return Problem(OAuthError.FlowRequiresAuthenticatedUser);
                break;
            default:
                return Problem(OAuthError.UnsupportedFlow);
        }

        return OAuthUtil.StartOAuth(provider, flow, activationToken);
    }
}
