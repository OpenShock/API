using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.API.Services.Account;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Edits a user
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpPatch("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ModifyUser([FromRoute] Guid userId, [FromBody] UserPatchDto body, [FromServices] IAccountService accountService, CancellationToken ct)
    {
        if (body.Name is not null)
        {
            await accountService.ChangeUsernameAsync(userId, body.Name, ignoreLimit: true, ct);
        }

        return Ok();
    }
}