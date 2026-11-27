using System.Diagnostics;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using Results = OpenShock.Common.Results;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Verify a pending email change of the current user using the token from the verification email.
    /// </summary>
    /// <remarks>
    /// Requires a session of the account that requested the change. The link is mailed to an address the
    /// account does not own yet, so opening it proves nothing about the account: accepted anonymously, a
    /// stranger's click would put their address on someone else's account.
    /// </remarks>
    /// <response code="200">Email change verified and applied</response>
    /// <response code="400">Token is invalid, already used, expired, or belongs to another account</response>
    /// <response code="409">The new email address was claimed by another account before verification completed</response>
    [HttpPost("email-change/verify")]
    [ApiVersion(1)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // EmailChangeNotFound
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // EmailChangeAlreadyInUse
    public Task<IActionResult> EmailVerify([FromQuery(Name = "token")] string token, CancellationToken cancellationToken)
        => VerifyPendingEmailChange(token, cancellationToken);

    /// <summary>
    /// Verify a pending email change of the current user. Deprecated: use POST /email-change/verify instead.
    /// </summary>
    /// <response code="200">Email change verified and applied</response>
    /// <response code="400">Token is invalid, already used, expired, or belongs to another account</response>
    /// <response code="409">The new email address was claimed by another account before verification completed</response>
    [Obsolete("Use POST /email-change/verify instead.")]
    [HttpPost("verify-email")]
    [ApiVersion(1)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // EmailChangeNotFound
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // EmailChangeAlreadyInUse
    public Task<IActionResult> EmailVerifyLegacy([FromQuery(Name = "token")] string token, CancellationToken cancellationToken)
        => VerifyPendingEmailChange(token, cancellationToken);

    private async Task<IActionResult> VerifyPendingEmailChange(string token, CancellationToken cancellationToken)
    {
        var result = await _accountService.TryVerifyEmailAsync(token, CurrentUser.Id, cancellationToken);

        return result switch
        {
            VerifyEmailSuccess => Ok(),
            Results.NotFound => Problem(AccountError.EmailChangeNotFound),
            EmailAlreadyInUse => Problem(AccountError.EmailChangeAlreadyInUse),
            _ => throw new UnreachableException()
        };
    }
}
