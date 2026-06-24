using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Models.Requests;
using OpenShock.API.Services.Turnstile;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
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
    public Task<IActionResult> PasswordResetInitiateV2([FromBody] PasswordResetRequestV2 body, [FromServices] ICloudflareTurnstileService turnstileService, CancellationToken cancellationToken)
        => PasswordResetInitiate(body, turnstileService, cancellationToken);

    /// <summary>
    /// Initiate a password reset. Deprecated: use POST /password-reset instead.
    /// </summary>
    /// <response code="200">Password reset email sent if the email is associated to an registered account</response>
    [Obsolete("Use POST /password-reset instead.")]
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [MapToApiVersion("2")]
    public Task<IActionResult> PasswordResetInitiateV2Legacy([FromBody] PasswordResetRequestV2 body, [FromServices] ICloudflareTurnstileService turnstileService, CancellationToken cancellationToken)
        => PasswordResetInitiate(body, turnstileService, cancellationToken);

    private async Task<IActionResult> PasswordResetInitiate(PasswordResetRequestV2 body, ICloudflareTurnstileService turnstileService, CancellationToken cancellationToken)
    {
        var turnstileError = await VerifyTurnstileAsync(turnstileService, body.TurnstileResponse, cancellationToken);
        if (turnstileError is not null) return turnstileError;

        await _accountService.CreatePasswordResetFlowAsync(body.Email);

        return Ok();
    }
}