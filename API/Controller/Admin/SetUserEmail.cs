using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Sets a users email to the supplied value
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("users/{userId}/email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserEmail([FromRoute(Name = "userId")] Guid userId, [FromBody] SetUserEmailRequestBody body, [FromServices] IAccountService accountService, CancellationToken cancellationToken)
    {
        var result = await accountService.ChangeEmail(userId, body.Email);

        return result.Match<IActionResult>(
            success => Ok(),
            taken => Problem(AdminError.EmailTaken),
            taken => Problem(AdminError.EmailInvalid),
            notfound => NotFound()
        );
    }

    public sealed class SetUserEmailRequestBody
    {
        public required string Email { get; init; }
    }
}