using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Models.Requests;
using OpenShock.API.Services.Turnstile;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Initiate a password reset. Retired: use POST /2/account/password-reset instead.
    /// </summary>
    [Obsolete("Retired. Use POST /2/account/password-reset instead.")]
    [HttpPost("reset-password")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [MapToApiVersion("2")]
    public IActionResult PasswordResetInitiateV2Legacy() => Problem(GoneError.EndpointRetired("POST /2/account/password-reset"));
    
    /// <summary>
    /// Initiate a password reset
    /// </summary>
    /// <response code="200">Password reset email sent if the email is associated to an registered account</response>
    [HttpPost("password-reset")]
    [EnableRateLimiting("auth")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [MapToApiVersion("2")]
    public async Task<IActionResult> PasswordResetInitiateV2([FromBody] PasswordResetRequestV2 body, [FromServices] ICloudflareTurnstileService turnstileService, CancellationToken cancellationToken)
    {
        var turnstileError = await VerifyTurnstileAsync(turnstileService, body.TurnstileResponse, cancellationToken);
        if (turnstileError is not null) return turnstileError;

        // Admin accounts must never be reached through a bypassed flow - the bypass exists for
        // automated tests, not as a way to send privileged reset mail without solving Turnstile.
        // The lookup runs only on the bypass path, so the normal path keeps its timing profile, and
        // the response stays the generic 200 so this does not become an admin-account oracle.
        if (HttpContext.IsBypassed(BypassTokenType.Turnstile)
            && await _accountService.IsPrivilegedEmailAsync(body.Email, cancellationToken))
        {
            _logger.LogWarning("Refused a bypassed password reset for a privileged account");
            return Ok();
        }

        await _accountService.CreatePasswordResetFlowAsync(body.Email);

        return Ok();
    }
}