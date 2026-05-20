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
    /// <response code="400">Token is invalid or the request has expired</response>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> EmailVerify([FromQuery(Name = "token")] string token, CancellationToken cancellationToken)
    {
        bool ok = await _accountService.TryVerifyEmailAsync(token, cancellationToken);

        return ok ? Ok() : Problem(AccountError.EmailChangeNotFound);
    }
}