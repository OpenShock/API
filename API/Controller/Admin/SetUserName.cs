using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Sets a users name to the supplied value
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("users/{userId}/name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserName([FromRoute(Name = "userId")] Guid userId, [FromBody] SetUserNameRequestBody body, [FromServices] IAccountService accountService, CancellationToken cancellationToken)
    {
        var result = await accountService.ChangeUsername(userId, body.Name, true);

        return result.Match<IActionResult>(
            success => Ok(),
            error => Problem(error.Value.Match(
                taken => AdminError.UsernameTaken,
                AccountError.UsernameInvalid,
                changed => throw new Exception("Failed to bypass username change ratelimit!")
            )),
            notfound => NotFound()
        );
    }

    public sealed class SetUserNameRequestBody
    {
        public required string Name { get; init; }
    }
}