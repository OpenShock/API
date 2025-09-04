using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Problems;
using System.Net.Mime;
using OpenShock.API.OAuth;
using OpenShock.Common.Authentication;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Start OAuth authorization for a given provider (login-or-create flow).
    /// </summary>
    /// <remarks>
    /// Initiates an OAuth challenge in "login-or-create" mode.  
    /// Returns <c>302</c> redirect to the provider authorization page.
    /// </remarks>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <response code="302">Redirect to the provider authorization page.</response>
    /// <response code="400">Unsupported or misconfigured provider.</response>
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> OAuthAuthorize([FromRoute] string provider)
    {
        if (!await _schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.UnsupportedProvider);

        if (User.Identities.Any(ident => string.Equals(ident.AuthenticationType, OpenShockAuthSchemes.UserSessionCookie,
                StringComparison.InvariantCultureIgnoreCase)))
        {
            return Problem(OAuthError.AnonymousOnlyEndpoint);
        }

        return OAuthUtil.StartOAuth(provider, OAuthFlow.LoginOrCreate);
    }
}