using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
using OpenShock.Common.Validation;
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
    public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameRequest body)
    {
        var result = await _accountService.ChangeUsernameAsync(CurrentUser.Id, body.Username,
            CurrentUser.Roles.Any(r => r is RoleType.Staff or RoleType.Admin or RoleType.System));

        return result.Match<IActionResult>(
            success => Ok(),
            usernametaken => Problem(AccountError.UsernameTaken),
            usernameerror => Problem(AccountError.UsernameInvalid(usernameerror)),
            recentlychanged => Problem(AccountError.UsernameRecentlyChanged),
            accountdeactivated => Problem(AccountError.AccountDeactivated),
            notfound => throw new Exception("Unexpected result, apparently our current user does not exist..."));
    }
}