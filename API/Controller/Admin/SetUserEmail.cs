using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using Results = OpenShock.Common.Results;

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

        return result switch
        {
            Results.Success => Ok(),
            EmailTaken => Problem(AdminError.EmailTaken),
            EmailInvalid => Problem(AdminError.EmailInvalid),
            Results.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    public sealed class SetUserEmailRequestBody
    {
        public required string Email { get; init; }
    }
}