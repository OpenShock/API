using System.Diagnostics;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.DataAnnotations;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Validation;
using Results = OpenShock.Common.Results;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Sets a users name to the supplied value
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="409">The target account is deactivated or not yet activated</response>
    [HttpPut("users/{userId}/name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> SetUserName([FromRoute(Name = "userId")] Guid userId, [FromBody] SetUserNameRequestBody body, [FromServices] IAccountService accountService, CancellationToken cancellationToken)
    {
        PedanticallyEnsureAdmin();

        var result = await accountService.ChangeUsernameAsync(userId, body.Name, actorId: CurrentUser.Id,
            ignoreLimit: true, cancellationToken);

        return result switch
        {
            Results.Success => Ok(),
            UsernameTaken => Problem(AdminError.UsernameTaken),
            UsernameError usernameError => Problem(AccountError.UsernameInvalid(usernameError)),
            RecentlyChanged => throw new UnreachableException("Failed to bypass username change ratelimit!"),
            AccountDeactivated => Problem(AdminError.UserDeactivated),
            Results.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    public sealed class SetUserNameRequestBody
    {
        [Username(true)]
        public required string Name { get; init; }
    }
}