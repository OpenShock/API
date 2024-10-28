using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Users;

public sealed partial class UsersController
{
    /// <summary>
    /// Delete the current user
    /// </summary>
    /// <response code="200">The user was successfully deleted.</response>
    [HttpGet("self")]
    public async Task<IActionResult> DeleteSelf()
    {
        if (CurrentUser.IsRank(RankType.Admin))
        {
            return Problem(AdminError.CannotDeletePrivledgedAccount);
        }

        _db.Users.Remove(CurrentUser.DbUser);
        await _db.SaveChangesAsync();

        return RespondSlimSuccess();
    }
}