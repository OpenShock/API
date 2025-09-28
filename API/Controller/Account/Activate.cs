using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Activate account
    /// </summary>
    /// <response code="200"></response>
    [HttpPost("activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Activate([FromQuery(Name = "token")] string token, CancellationToken cancellationToken)
    {
        bool ok = await _accountService.TryActivateAccountAsync(token, cancellationToken);
        
        return ok ? Ok() : Problem(AccountError.AccountActivationNotFound);
    }
}