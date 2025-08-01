using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpGet("blacklist/emailProviders")]
    public IAsyncEnumerable<EmailProviderBlacklist> ListEmailProviderBlacklist()
    {
        return _db.EmailProviderBlacklists.AsNoTracking().AsAsyncEnumerable();
    }
}
