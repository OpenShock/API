using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Verify account email
    /// </summary>
    /// <response code="200"></response>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> EmailVerify([FromQuery(Name = "token")] string token, CancellationToken cancellationToken)
    {
        bool ok = await _accountService.TryVerifyEmailAsync(token, cancellationToken);
        
        return Ok();
    }
}