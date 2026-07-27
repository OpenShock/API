using System.Diagnostics;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Validation;
using Results = OpenShock.Common.Results;

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
        var result = await _accountService.ChangeUsernameAsync(CurrentUser.Id, body.Username, actorId: CurrentUser.Id,
            ignoreLimit: CurrentUser.Roles.Any(r => r is RoleType.Staff or RoleType.Admin or RoleType.System));

        return result switch
        {
            Results.Success => Ok(),
            UsernameTaken => Problem(AccountError.UsernameTaken),
            UsernameError usernameError => Problem(AccountError.UsernameInvalid(usernameError)),
            RecentlyChanged => Problem(AccountError.UsernameRecentlyChanged),
            AccountDeactivated => Problem(AccountError.AccountDeactivated),
            Results.NotFound => throw new Exception("Unexpected result, apparently our current user does not exist..."),
            _ => throw new UnreachableException()
        };
    }
}