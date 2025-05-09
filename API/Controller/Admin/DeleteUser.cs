﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid userId)
    {
        var user = await _db.Users.Where(x => x.Id == userId).FirstOrDefaultAsync();
        if (user == null)
        {
            return Problem(AdminError.UserNotFound);
        }

        if (user.Roles.Any(r => r is RoleType.Admin or RoleType.System))
        {
            return Problem(AdminError.CannotDeletePrivledgedAccount);
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok();
    }
}