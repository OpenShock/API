using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Models.Response;
using OpenShock.Common.Problems;
using System.Net.Mime;
using System.Security.Claims;
using OpenShock.API.OAuth;

namespace OpenShock.API.Controller.OAuth;

public sealed partial class OAuthController
{
    /// <summary>
    /// Retrieve short-lived OAuth handoff information for the current flow.
    /// </summary>
    /// <remarks>
    /// Returns identity details from the external provider (e.g., email, display name) along with the flow expiry.
    /// This endpoint is authenticated via the temporary OAuth flow cookie and is only accessible to the user who initiated the flow.
    /// </remarks>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <response code="200">Handoff data returned.</response>
    /// <response code="400">Flow not found or provider mismatch.</response>
    [ResponseCache(NoStore = true)]
    [EnableRateLimiting("auth")]
    [HttpGet("{provider}/data")]
    [ProducesResponseType<OAuthDataResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> OAuthGetData([FromRoute] string provider)
    {
        var result = await ValidateOAuthFlowAsync(provider);
        if (!result.TryPickT0(out var auth, out var response))
        {
            return response;
        }

        return Ok(new OAuthDataResponse
        {
            Provider = auth.Provider,
            Email = auth.Principal.FindFirst(ClaimTypes.Email)?.Value,
            DisplayName = auth.Principal.FindFirst(ClaimTypes.Name)?.Value,
            ExpiresAt = auth.Properties.ExpiresUtc!.Value.UtcDateTime
        });
    }
}
