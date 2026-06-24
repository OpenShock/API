using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.DataAnnotations;

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
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> ResendActivation([FromBody] ResendActivationRequest body, CancellationToken cancellationToken)
    {
        await _accountService.ResendActivationEmailAsync(body.Email, cancellationToken);
        return LegacyEmptyOk("Activation email has been sent if the email is associated to an unactivated account");
    }

    public sealed class ResendActivationRequest
    {
        [EmailAddress(true)]
        public required string Email { get; init; }
    }
}
