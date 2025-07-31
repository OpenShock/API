using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpGet("blacklists/emailProviders")]
    public async Task<EmailProviderBlacklist[]> ListEmailProviderBlacklist()
    {
        return await _db.EmailProviderBlacklists.AsNoTracking().ToArrayAsync();
    }
}
