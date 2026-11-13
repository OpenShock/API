using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.Account;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using OpenShock.Internal.Common.Problems;
using Results = OpenShock.Common.Results;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Resend the account activation email for the account this browser just signed up
    /// </summary>
    /// <remarks>
    /// Identifies the account by the activation-resend cookie set on signup, not by an email address,
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
        {
            // Tampered or expired (a no-op when there is no cookie), so the frontend stops offering the button.
            ActivationResendCookie.Remove(HttpContext);
            return Problem(AccountError.AccountActivationResendUnavailable);
        }

        var result = await _accountService.ResendActivationEmailAsync(userId.Value, cancellationToken);

        // Already activated, deactivated, or gone: the cookie has nothing left to do.
        if (result is AccountDeactivated or Results.NotFound)
            ActivationResendCookie.Remove(HttpContext);

        return result switch
        {
            Results.Success => Ok(),
            TooManyActivationResends => Problem(AccountError.AccountActivationResendTooMany),
            AccountDeactivated or Results.NotFound => Problem(AccountError.AccountActivationResendUnavailable)
        };
    }
}
