using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Extensions;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Audit;

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
        [FromServices] IAccountService accountService,
        [FromServices] IAuditService auditService)
    {
        var result = await accountService.DeleteAccountAsync(CurrentUser.Id, userId);
        return await result.Match<Task<IActionResult>>(
            async success =>
            {
                await auditService.LogAsync(
                    userId,
                    AuditAction.AccountDeleted,
                    ipAddress: HttpContext.GetRemoteIP(),
                    userAgent: HttpContext.GetUserAgent(),
                    actorId: CurrentUser.Id);
                return Ok("Account deleted");
            },
            cannotDeletePrivledged => Task.FromResult<IActionResult>(Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount)),
            unauthorized => Task.FromResult<IActionResult>(Problem(AccountActivationError.Unauthorized)),
            notFound => Task.FromResult<IActionResult>(NotFound("User not found"))
        );
    }
}
