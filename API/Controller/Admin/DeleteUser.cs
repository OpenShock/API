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
        return result switch
        {
            Results.Success => Ok("Account deleted"),
            CannotDeletePrivilegedAccount => Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount),
            AccountSvc.Unauthorized => Problem(AccountActivationError.Unauthorized),
            Results.NotFound => NotFound("User not found"),
            _ => throw new UnreachableException()
        };
    }
}