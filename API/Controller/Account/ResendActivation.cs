using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.Account;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Internal.Common.Problems;
using Results = OpenShock.Common.Results;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Resend the account activation email for the account this browser just signed up
    /// </summary>
    /// <remarks>
    /// Identifies the account by the HttpOnly activation-resend cookie set on signup, not by an email address,
    /// so it can only target an account created from this browser and can't be used to probe registered emails.
    /// </remarks>
    /// <response code="200">Activation email enqueued</response>
    /// <response code="400">No pending activation for this browser (missing/expired cookie, or already activated)</response>
    /// <response code="429">An activation email was sent too recently</response>
    [HttpPost("activate/resend")]
    [EnableRateLimiting("auth")]
    [MapToApiVersion("1")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // AccountActivationResendUnavailable
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status429TooManyRequests, MediaTypeNames.Application.ProblemJson)] // AccountActivationResendTooMany
    public async Task<IActionResult> ResendActivation(CancellationToken cancellationToken)
    {
        if (!ActivationResendCookie.TryGetUserId(HttpContext, out var userId))
            return Problem(AccountError.AccountActivationResendUnavailable);

        var result = await _accountService.ResendActivationEmailAsync(userId.Value, cancellationToken);

        switch (result)
        {
            case Results.Success:
                return Ok();
            case TooManyActivationResends:
                return Problem(AccountError.AccountActivationResendTooMany);
            default:
                // Already activated, deactivated, or gone: the cookie has nothing left to do.
                ActivationResendCookie.Remove(HttpContext);
                return Problem(AccountError.AccountActivationResendUnavailable);
        }
    }
}
