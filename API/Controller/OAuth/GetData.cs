using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Extensions;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using System.Net.Mime;
using System.Security.Claims;

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
        if (!await _schemeProvider.IsSupportedOAuthScheme(provider))
            return Problem(OAuthError.UnsupportedProvider);

        // Temp external principal (set by OAuth handler with SignInScheme=OAuthFlowScheme, SaveTokens=true)
        var auth = await HttpContext.AuthenticateAsync(OpenShockAuthSchemes.OAuthFlowScheme);
        if (!auth.Succeeded || auth.Principal is null)
            return Problem(OAuthError.FlowNotFound);

        // Read identifiers from claims (no props.Items)
        var flowIdClaim = auth.Principal.FindFirst("flow_id")?.Value;
        var providerClaim = auth.Principal.FindFirst("provider")?.Value;

        if (string.IsNullOrWhiteSpace(flowIdClaim) || string.IsNullOrWhiteSpace(providerClaim))
        {
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Problem(OAuthError.FlowNotFound);
        }

        // Defensive: ensure the snapshot belongs to this provider
        if (providerClaim != provider)
        {
            // Optional: you may also delete the cookie if you consider this a poisoned flow
            await HttpContext.SignOutAsync(OpenShockAuthSchemes.OAuthFlowScheme);
            return Problem(OAuthError.ProviderMismatch);
        }

        return Ok(new OAuthDataResponse
        {
            Provider = providerClaim,
            Email = auth.Principal.FindFirst(ClaimTypes.Email)?.Value,
            DisplayName = auth.Principal.FindFirst(ClaimTypes.Name)?.Value,
            ExpiresAt = auth.Ticket.Properties.ExpiresUtc!.Value.UtcDateTime
        });
    }
}
