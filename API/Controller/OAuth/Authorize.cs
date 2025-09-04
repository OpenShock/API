using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using System.Net.Mime;

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
            return Problem(OAuthError.ProviderNotSupported);

        // Kick off provider challenge in "login-or-create" mode.
        var props = new AuthenticationProperties
        {
            RedirectUri = $"/1/oauth/{provider}/handoff",
            Items = { { "flow", AuthConstants.OAuthLoginOrCreateFlow } }
        };

        return Challenge(props, provider);
    }
}