using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;

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
    public async Task<IActionResult> DeleteUser(
        [FromRoute] Guid userId,
        [FromQuery][MaxLength(ApiHardLimits.AuditReasonMaxLength)] string? reason,
        [FromServices] IAccountService accountService)
    {
        PedanticallyEnsureAdmin();

        var result = await accountService.DeleteAccountAsync(CurrentUser.Id, userId, reason);
        return result.Match<IActionResult>(
            success => Ok("Account deleted"),
            cannotDeletePrivledged => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            unauthorized => Problem(AccountActivationError.Unauthorized),
            notFound => NotFound("User not found")
        );
    }
}