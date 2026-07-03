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
    /// Initiate an email change for the current user. A verification link is sent to the new
    /// address; the change is not applied until that link is opened.
    /// </summary>
    /// <param name="body"></param>
    /// <param name="auditService"></param>
    [HttpPost("email-change")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // EmailChangeUnchanged
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)] // AccountOAuthOnly
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // PasswordChangeInvalidPassword
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // EmailChangeAlreadyInUse
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status429TooManyRequests, MediaTypeNames.Application.ProblemJson)] // EmailChangeTooMany
    // notActivated / deactivated / notFound are blocked by UserSessionAuthentication before reaching this controller.
    public async Task<IActionResult> ChangeEmail(
        [FromBody] ChangeEmailRequest body,
        [FromServices] IAuditService auditService)
    {
        if (string.IsNullOrEmpty(CurrentUser.PasswordHash))
        {
            return Problem(AccountError.AccountOAuthOnly);
        }

        if (!HashingUtils.VerifyPassword(body.CurrentPassword, CurrentUser.PasswordHash).Verified)
        {
            return Problem(AccountError.PasswordChangeInvalidPassword);
        }

        var result = await _accountService.CreateEmailChangeFlowAsync(CurrentUser.Id, body.Email);

        return await result.Match<Task<IActionResult>>(
            async success =>
            {
                await auditService.LogAsync(
                    CurrentUser.Id,
                    AuditAction.EmailChangeRequested,
                    ipAddress: HttpContext.GetRemoteIP(),
                    userAgent: HttpContext.GetUserAgent(),
                    metadata: new EmailChangeRequestedMetadata(body.Email));
                return Ok();
            },
            alreadyInUse => Task.FromResult<IActionResult>(Problem(AccountError.EmailChangeAlreadyInUse)),
            unchanged => Task.FromResult<IActionResult>(Problem(AccountError.EmailChangeUnchanged)),
            tooMany => Task.FromResult<IActionResult>(Problem(AccountError.EmailChangeTooMany)),
            notActivated => throw new UnreachableException("Authenticated user is not activated"),
            deactivated => throw new UnreachableException("Authenticated user is deactivated"),
            notFound => throw new UnreachableException("Authenticated user not found in database"));
    }
}
