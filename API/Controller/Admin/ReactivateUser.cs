using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.Results;
using AccountSvc = OpenShock.API.Services.Account;
using Results = OpenShock.Common.Results;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Reactivates a user
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("users/{userId}/reactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReactivateUser(
        [FromRoute] Guid userId,
        [FromQuery][MaxLength(HardLimits.AuditReasonMaxLength)] string? reason,
        [FromServices] IAccountService accountService)
    {
        PedanticallyEnsureAdmin();

        var reactivationResult = await accountService.ReactivateAccountAsync(CurrentUser.Id, userId, reason);
        return reactivationResult switch
        {
            Success => Ok("Account reactivated"),
            AccountSvc.Unauthorized => Problem(AccountActivationError.Unauthorized),
            Results.NotFound => NotFound("User not found"),
            _ => throw new UnreachableException()
        };
    }
}