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
    /// Reactivates a user
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("users/{userId}/reactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReactivateUser([FromRoute] Guid userId, IAccountService accountService)
    {
        var reactivationResult = await accountService.ReactivateAccountAsync(CurrentUser.Id, userId);
        return reactivationResult.Match(
            success => Ok("Account reactivated"),
            unauthorized => Problem(AccountActivationError.Unauthorized),
            notFound => NotFound("User not found")
        );
    }
}