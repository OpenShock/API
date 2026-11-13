using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
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
        [FromQuery][MaxLength(ApiHardLimits.AuditReasonMaxLength)] string? reason,
        [FromServices] AccountSvc.IAccountService accountService,
        CancellationToken cancellationToken)
    {
        PedanticallyEnsureAdmin();

        var deactivationResult = await accountService.DeactivateAccountAsync(CurrentUser.Id, userId, deleteLater, reason, cancellationToken);
        return deactivationResult switch
        {
            Results.Success => Ok("Account deactivated"),
            AccountSvc.CannotDeactivatePrivilegedAccount => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            AccountSvc.AccountDeactivationAlreadyInProgress => Problem(AccountActivationError.AlreadyDeactivated),
            AccountSvc.Unauthorized => Problem(AccountActivationError.Unauthorized),
            Results.NotFound => NotFound("User not found")
        };
    }
}
