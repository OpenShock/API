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
    /// Reactivates a user
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("users/{userId}/reactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReactivateUser(
        [FromRoute] Guid userId,
        [FromServices] IAccountService accountService,
        [FromServices] IAuditService auditService)
    {
        var reactivationResult = await accountService.ReactivateAccountAsync(CurrentUser.Id, userId);
        return await reactivationResult.Match<Task<IActionResult>>(
            async success =>
            {
                await auditService.LogAsync(
                    userId,
                    AuditAction.AccountReactivated,
                    ipAddress: HttpContext.GetRemoteIP(),
                    userAgent: HttpContext.GetUserAgent(),
                    actorId: CurrentUser.Id);
                return Ok("Account reactivated");
            },
            unauthorized => Task.FromResult<IActionResult>(Problem(AccountActivationError.Unauthorized)),
            notFound => Task.FromResult<IActionResult>(NotFound("User not found"))
        );
    }
}