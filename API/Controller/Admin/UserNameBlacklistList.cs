using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpGet("blacklist/usernames")]
    public IAsyncEnumerable<UserNameBlacklist> ListUsernameBlacklist()
    {
        return _db.UserNameBlacklists.AsNoTracking().AsAsyncEnumerable();
    }
}
