using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Extensions;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using System.Net.Mime;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Start linking an OAuth provider to the current account.
    /// </summary>
    /// <remarks>
    /// Initiates the OAuth flow (link mode) for a given provider.  
    /// On success this returns a <c>302 Found</c> to the provider's authorization page.
    /// After consent, the OAuth middleware will call the internal callback and finally
    /// redirect to <c>/1/oauth/{provider}/handoff</c>.
    /// </remarks>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <param name="schemeProvider"></param>
    /// <response code="302">Redirect to the provider authorization page.</response>
    /// <response code="400">Unsupported or misconfigured provider.</response>
    [HttpGet("connections/{provider}/link")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> AddOAuthConnection([FromRoute] string provider, [FromServices] IAuthenticationSchemeProvider schemeProvider)
    {
        if (!await schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.ProviderNotSupported);

        // Kick off provider challenge in "link" mode.
        // Redirect URI is our handoff endpoint which decides next UI step.
        var props = new AuthenticationProperties {
            RedirectUri = $"/1/oauth/{provider}/handoff",
            Items = { { "flow", AuthConstants.OAuthLinkFlow } }
        };

        return Challenge(props, provider);
    }
}