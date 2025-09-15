using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Models.Response;
using OpenShock.Common.Problems;
using System.Net.Mime;
using System.Security.Claims;
using OpenShock.API.OAuth;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Provides temporary OAuth handoff details for the active signup flow.
    /// </summary>
    /// <remarks>
    /// Returns basic identity information from the external provider (e.g., email, display name)
    /// together with the expiration time of the current flow.
    /// Access to this endpoint requires the temporary OAuth flow cookie and is restricted to the user
    /// who initiated the flow.
    /// </remarks>
    /// <param name="provider">The provider key (e.g. <c>discord</c>).</param>
    /// <response code="200">Handoff information returned successfully.</response>
    /// <response code="400">No active flow found, or the provider did not match.</response>
    [ResponseCache(NoStore = true)]
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/signup-data")]
    [ProducesResponseType<OAuthSignupDataResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> OAuthSignupGetData([FromRoute] string provider)
    {
        if (User.HasOpenShockUserIdentity())
        {
            return Problem(OAuthError.AnonymousOnlyEndpoint);
        }
        
        var result = await ValidateOAuthFlowAsync();
        if (!result.TryPickT0(out var auth, out var error))
        {
            return error switch
            {
                OAuthValidationError.FlowStateMissing => Problem(OAuthError.FlowNotFound),
                _ => Problem(OAuthError.InternalError)
            };
        }

        if (auth.Provider != provider)
        {
            return Problem(OAuthError.ProviderMismatch);
        }

        if (auth.Flow != OAuthFlow.LoginOrCreate)
        {
            return Problem(OAuthError.FlowMismatch);
        }

        return Ok(new OAuthSignupDataResponse
        {
            Provider = auth.Provider,
            Email = auth.Principal.FindFirst(ClaimTypes.Email)?.Value,
            DisplayName = auth.ExternalAccountDisplayName ?? auth.ExternalAccountName,
            ExpiresAt = auth.Properties.ExpiresUtc!.Value.UtcDateTime
        });
    }
}
