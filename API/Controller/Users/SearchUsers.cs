using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.UserService;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Users;

public sealed partial class UsersController
{
    [ProducesResponseType<BasicUserInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [HttpGet("search/{username}")]
    public async Task<IActionResult> SearchUserDirect([FromRoute] string username, [FromServices] IUserService userService, CancellationToken cancellationToken)
    {
        var user = await userService.SearchUserDirect(username, cancellationToken);
        return user is null ? Problem(UserError.UserNotFound) : Ok(user);
    }
}