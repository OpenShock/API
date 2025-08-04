using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpGet("blacklist/emailProviders")]
    public async IAsyncEnumerable<EmailProviderBlacklistDto> ListEmailProviderBlacklist([FromQuery] string? match)
    {
        if (string.IsNullOrWhiteSpace(match))
        {
            match = null;
        }
        else
        {
            if (!MailAddress.TryCreate(match, out var parsed)) yield break;
            
            match = parsed.Host.ToLowerInvariant();
        }
        
        await foreach (var item in _db.EmailProviderBlacklists.AsNoTracking().AsAsyncEnumerable())
        {
            if (match is not null && !match.EndsWith(item.Domain)) continue;
            
            yield return new EmailProviderBlacklistDto
            {
                Id = item.Id,
                Domain = item.Domain,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
