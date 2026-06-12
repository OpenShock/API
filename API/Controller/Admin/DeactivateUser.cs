using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Audit;

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
        [FromServices] IAccountService accountService,
        [FromServices] IAuditService auditService)
    {
        var deactivationResult = await accountService.DeactivateAccountAsync(CurrentUser.Id, userId, deleteLater);
        return await deactivationResult.Match<Task<IActionResult>>(
            async success =>
            {
                await auditService.LogAsync(
                    userId,
                    AuditAction.AccountDeactivated,
                    ipAddress: HttpContext.GetRemoteIP(),
                    userAgent: HttpContext.GetUserAgent(),
                    metadata: new AccountDeactivatedMetadata(deleteLater),
                    actorId: CurrentUser.Id);
                return Ok("Account deactivated");
            },
            cannotDeactivatePrivledged => Task.FromResult<IActionResult>(Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount)),
            alreadyDeactivated => Task.FromResult<IActionResult>(Problem(AccountActivationError.AlreadyDeactivated)),
            unauthorized => Task.FromResult<IActionResult>(Problem(AccountActivationError.Unauthorized)),
            notFound => Task.FromResult<IActionResult>(NotFound("User not found"))
        );
    }
}