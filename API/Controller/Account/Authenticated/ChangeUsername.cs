using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Audit;
using System.Net.Mime;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Change the username of the current user
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("username")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // UsernameTaken
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // UsernameInvalid
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // UsernameRecentlyChanged
    public async Task<IActionResult> ChangeUsername(
        [FromBody] ChangeUsernameRequest body,
        [FromServices] IAuditService auditService)
    {
        var oldUsername = CurrentUser.Name;
        var result = await _accountService.ChangeUsernameAsync(CurrentUser.Id, body.Username,
            CurrentUser.Roles.Any(r => r is RoleType.Staff or RoleType.Admin or RoleType.System));

        return await result.Match<Task<IActionResult>>(
            async success =>
            {
                await auditService.LogAsync(
                    CurrentUser.Id,
                    AuditAction.UsernameChanged,
                    ipAddress: HttpContext.GetRemoteIP(),
                    userAgent: HttpContext.GetUserAgent(),
                    metadata: new UsernameChangedMetadata(oldUsername, body.Username));
                return Ok();
            },
            usernametaken => Task.FromResult<IActionResult>(Problem(AccountError.UsernameTaken)),
            usernameerror => Task.FromResult<IActionResult>(Problem(AccountError.UsernameInvalid(usernameerror))),
            recentlychanged => Task.FromResult<IActionResult>(Problem(AccountError.UsernameRecentlyChanged)),
            accountdeactivated => Task.FromResult<IActionResult>(Problem(AccountError.AccountDeactivated)),
            notfound => throw new Exception("Unexpected result, apparently our current user does not exist..."));
    }
}