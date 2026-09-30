using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mime;
using Z.EntityFramework.Plus;
using Results = OpenShock.Common.Results;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Sets a users password to the supplied value
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="409">The target account is deactivated or not yet activated</response>
    [HttpPut("users/{userId}/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> SetUserPassword([FromRoute(Name = "userId")] Guid userId, [FromBody] SetUserPasswordRequestBody body, [FromServices] IAccountService accountService, CancellationToken cancellationToken)
    {
        var result = await accountService.ChangePasswordAsync(userId, body.Password, actorId: CurrentUser.Id);

        return result switch
        {
            Results.Success => Ok(),
            AccountNotActivated => Problem(AdminError.UserNotActivated),
            AccountDeactivated => Problem(AdminError.UserDeactivated),
            Results.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    public sealed class SetUserPasswordRequestBody
    {
        public required string Password { get; init; }
    }
}