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
    /// Deletes a user
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid userId, IAccountService accountService)
    {
        var result = await accountService.DeleteAccountAsync(CurrentUser.Id, userId);
        return result.Match(
            success => Ok("Account deleted"),
            cannotDeletePrivledged => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            unauthorized => Problem(AccountActivationError.Unauthorized),
            notFound => NotFound("User not found")
        );
    }
}