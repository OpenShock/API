using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Utils;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
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
    [ProducesSlimSuccess]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid userId)
    {
        var user = await _db.Users.Where(x => x.Id == userId).FirstOrDefaultAsync();
        if (user == null)
        {
            return Problem(AdminError.UserNotFound);
        }

        if (user.Rank >= RankType.Admin)
        {
            return Problem(AdminError.CannotDeletePrivledgedAccount);
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return RespondSlimSuccess();
    }
}