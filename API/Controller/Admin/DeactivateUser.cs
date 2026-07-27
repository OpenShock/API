using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using AccountSvc = OpenShock.API.Services.Account;
using Results = OpenShock.Common.Results;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Deactivates a user
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("users/{userId}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateUser(
        [FromRoute] Guid userId,
        [FromQuery(Name = "deleteLater")] bool deleteLater,
        [FromQuery][MaxLength(HardLimits.AuditReasonMaxLength)] string? reason,
        [FromServices] IAccountService accountService)
    {
        PedanticallyEnsureAdmin();

        var deactivationResult = await accountService.DeactivateAccountAsync(CurrentUser.Id, userId, deleteLater, reason);
        return deactivationResult switch
        {
            Results.Success => Ok("Account deactivated"),
            CannotDeactivatePrivilegedAccount => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            AccountDeactivationAlreadyInProgress => Problem(AccountActivationError.AlreadyDeactivated),
            AccountSvc.Unauthorized => Problem(AccountActivationError.Unauthorized),
            Results.NotFound => NotFound("User not found"),
            _ => throw new UnreachableException()
        };
    }
}