using System.Diagnostics;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Change the password of the current user
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("password")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // PasswordChangeInvalidPassword
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // PasswordNotSet
    // notActivated / deactivated / notFound are blocked by UserSessionAuthentication before reaching this controller.
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest body,
        [FromServices] IAuditService auditService)
    {
        // OAuth-only accounts that have never set a password must go through the email-confirmed
        // /password/set flow rather than silently setting one through this endpoint.
        if (string.IsNullOrEmpty(CurrentUser.PasswordHash))
        {
            return Problem(AccountError.PasswordNotSet);
        }

        if (!HashingUtils.VerifyPassword(body.CurrentPassword, CurrentUser.PasswordHash).Verified)
        {
            return Problem(AccountError.PasswordChangeInvalidPassword);
        }
        
        var result = await _accountService.ChangePasswordAsync(CurrentUser.Id, body.NewPassword);

        return await result.Match<Task<IActionResult>>(
            async success =>
            {
                await auditService.LogAsync(
                    CurrentUser.Id,
                    AuditAction.PasswordChanged,
                    ipAddress: HttpContext.GetRemoteIP(),
                    userAgent: HttpContext.GetUserAgent());
                return Ok();
            },
            notActivated => throw new UnreachableException("Authenticated user is not activated"),
            deactivated => throw new UnreachableException("Authenticated user is deactivated"),
            notFound => throw new UnreachableException("Authenticated user not found in database"));

    }
}
