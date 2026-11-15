using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Errors;
using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Activate account
    /// </summary>
    [HttpPost("activate")]
    [ApiVersion(1)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Activate([FromQuery(Name = "token")] string token, CancellationToken cancellationToken)
    {
        bool ok = await _accountService.TryActivateAccountAsync(token, cancellationToken);
        
        return ok ? Ok() : Problem(AccountError.AccountActivationNotFound);
    }
}
