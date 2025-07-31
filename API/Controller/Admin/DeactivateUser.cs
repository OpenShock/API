using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using Z.EntityFramework.Plus;

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
    public async Task<IActionResult> DeactivateUser([FromRoute] Guid userId, [FromQuery(Name="deleteLater")] bool deleteLater, IAccountService accountService)
    {
        var deactivationResult = await accountService.DeactivateAccountAsync(CurrentUser.Id, userId, deleteLater);
        return deactivationResult.Match(
            success => Ok("Account deactivated"),
            cannotDeactivatePrivledged => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            alreadyDeactivated => Problem(AccountActivationError.AlreadyDeactivated),
            unauthorized => Problem(AccountActivationError.Unauthorized),
            notFound => NotFound("User not found")
        );
    }
}