using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Extensions;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Audit;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Deactivate currently logged in account
    /// </summary>
    /// <response code="204">Done.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // CannotDeactivatePrivledgedAccount
    public async Task<IActionResult> Deactivate([FromServices] IAuditService auditService)
    {
        var deactivationResult = await _accountService.DeactivateAccountAsync(CurrentUser.Id, CurrentUser.Id, deleteLater: true);
        return await deactivationResult.Match<Task<IActionResult>>(
            async success =>
            {
                await auditService.LogAsync(
                    CurrentUser.Id,
                    AuditAction.AccountDeactivated,
                    ipAddress: HttpContext.GetRemoteIP(),
                    userAgent: HttpContext.GetUserAgent(),
                    metadata: new AccountDeactivatedMetadata(DeleteLater: true));
                return NoContent();
            },
            cannotDeactivatePrivledged => Task.FromResult<IActionResult>(Problem(AccountActivationError.CannotDeactivateOrDeletePrivledgedAccount)),
            alreadyDeactivated => Task.FromResult<IActionResult>(Problem(AccountActivationError.AlreadyDeactivated)),
            unauthorized => Task.FromResult<IActionResult>(Problem(AccountActivationError.Unauthorized)),
            notFound => throw new Exception("This is not supposed to happen, wtf?")
        );
    }
}
