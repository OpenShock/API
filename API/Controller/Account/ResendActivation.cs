using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.Turnstile;
using OpenShock.Common.Constants;
using OpenShock.Common.DataAnnotations;
using OpenShock.Common.Problems;
using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Resend the account activation email
    /// </summary>
    /// <response code="200">Activation email sent if the email is associated to an unactivated account</response>
    [HttpPost("activate/resend")]
    [EnableRateLimiting("auth")]
    [Consumes(MediaTypeNames.Application.Json)]
    [MapToApiVersion("1")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> ResendActivation([FromBody] ResendActivationRequest body, [FromServices] ICloudflareTurnstileService turnstileService, CancellationToken cancellationToken)
    {
        // Unauthenticated and it puts mail in a third party's inbox, so it carries the same turnstile
        // requirement as password-reset initiation; the rate limiter alone gates nothing but volume.
        var turnstileError = await VerifyTurnstileAsync(turnstileService, body.TurnstileResponse, cancellationToken);
        if (turnstileError is not null) return turnstileError;

        await _accountService.ResendActivationEmailAsync(body.Email, cancellationToken);
        return Ok();
    }

    public sealed class ResendActivationRequest
    {
        [OpenShock.Common.DataAnnotations.EmailAddress(true)]
        public required string Email { get; init; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(ApiHardLimits.MaxTurnstileResponseTokenLength)]
        public required string TurnstileResponse { get; init; }
    }
}
