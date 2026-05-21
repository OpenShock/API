using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Verify a pending email change using the token from the verification email.
    /// </summary>
    /// <response code="200">Email change verified and applied</response>
    /// <response code="400">Token is invalid, already used, or the request has expired</response>
    /// <response code="409">The new email address was claimed by another account before verification completed</response>
    [HttpPost("email-change/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> EmailVerify([FromQuery(Name = "token")] string token, CancellationToken cancellationToken)
    {
        var result = await _accountService.TryVerifyEmailAsync(token, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(),
            notFound => Problem(AccountError.EmailChangeNotFound),
            emailTaken => Problem(AccountError.EmailChangeAlreadyInUse));
    }
}