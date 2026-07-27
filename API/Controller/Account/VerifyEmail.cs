using System;
using System.Diagnostics;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using Results = OpenShock.Common.Results;

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
    public Task<IActionResult> EmailVerify([FromQuery(Name = "token")] string token, CancellationToken cancellationToken)
        => VerifyPendingEmailChange(token, cancellationToken);

    /// <summary>
    /// Verify a pending email change. Deprecated: use POST /email-change/verify instead.
    /// </summary>
    /// <response code="200">Email change verified and applied</response>
    /// <response code="400">Token is invalid, already used, or the request has expired</response>
    /// <response code="409">The new email address was claimed by another account before verification completed</response>
    [Obsolete("Use POST /email-change/verify instead.")]
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    [MapToApiVersion("1")]
    public Task<IActionResult> EmailVerifyLegacy([FromQuery(Name = "token")] string token, CancellationToken cancellationToken)
        => VerifyPendingEmailChange(token, cancellationToken);

    private async Task<IActionResult> VerifyPendingEmailChange(string token, CancellationToken cancellationToken)
    {
        var result = await _accountService.TryVerifyEmailAsync(token, cancellationToken);

        return result switch
        {
            VerifyEmailSuccess => Ok(),
            Results.NotFound => Problem(AccountError.EmailChangeNotFound),
            EmailAlreadyInUse => Problem(AccountError.EmailChangeAlreadyInUse),
            _ => throw new UnreachableException()
        };
    }
}