using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Services.Account;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mime;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Sets a users password to the supplied value
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("users/{userId}/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserPassword([FromRoute(Name = "userId")] Guid userId, [FromBody] SetUserPasswordRequestBody body, [FromServices] IAccountService accountService, CancellationToken cancellationToken)
    {
        var passwordResetComplete = await accountService.ChangePassword(userId, body.Password);

        return passwordResetComplete.Match<IActionResult>(
            success => Ok(),
            notFound => NotFound());
    }

    public sealed class SetUserPasswordRequestBody
    {
        public required string Password { get; init; }
    }
}