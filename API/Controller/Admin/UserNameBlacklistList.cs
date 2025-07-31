using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpGet("blacklists/usernames")]
    public async Task<UserNameBlacklist[]> ListUsernameBlacklist()
    {
        return await _db.UserNameBlacklists.AsNoTracking().ToArrayAsync();
    }
}
