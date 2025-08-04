using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Verify account email
    /// </summary>
    /// <response code="200"></response>
    [HttpPost("verify/email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)]
    [MapToApiVersion("2")]
    public async Task<IActionResult> EmailVerify([FromBody] AccountVerifyEmailRequest body, CancellationToken cancellationToken)
    {
        bool ok = await _accountService.TryVerifyEmailAsync(body.Secret, cancellationToken);
        
        return Ok();
    }
}