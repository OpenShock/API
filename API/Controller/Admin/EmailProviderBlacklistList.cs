using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpGet("blacklist/emailProviders")]
    public async IAsyncEnumerable<EmailProviderBlacklistDto> ListEmailProviderBlacklist()
    {
        await foreach (var item in _db.EmailProviderBlacklists.AsNoTracking().AsAsyncEnumerable())
        {
            yield return new EmailProviderBlacklistDto
            {
                Id = item.Id,
                Domain = item.Domain,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
