using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpGet("blacklist/usernames")]
    public async IAsyncEnumerable<UserNameBlacklistDto> ListUsernameBlacklist([FromQuery] string? match)
    {
        match = string.IsNullOrWhiteSpace(match) ? null : match.Trim().ToLowerInvariant();

        await foreach (var item in _db.UserNameBlacklists.AsNoTracking().AsAsyncEnumerable())
        {
            if (match is not null && !item.IsMatch(match)) continue;
            
            yield return new UserNameBlacklistDto {
                Id = item.Id,
                Value = item.Value,
                MatchType = item.MatchType,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
