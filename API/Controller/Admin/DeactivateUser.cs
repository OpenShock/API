using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;

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
        [FromServices] IAccountService accountService)
    {
        PedanticallyEnsureAdmin();

        var deactivationResult = await accountService.DeactivateAccountAsync(CurrentUser.Id, userId, deleteLater, reason);
        return deactivationResult.Match<IActionResult>(
            success => Ok("Account deactivated"),
            cannotDeactivatePrivledged => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            alreadyDeactivated => Problem(AccountActivationError.AlreadyDeactivated),
            unauthorized => Problem(AccountActivationError.Unauthorized),
            notFound => NotFound("User not found")
        );
    }
}