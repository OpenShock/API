using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpGet("blacklist/usernames")]
    [ApiVersion(1)]
    public async IAsyncEnumerable<UserNameBlacklistDto> ListUsernameBlacklist([FromQuery(Name="match")] string? match, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        PedanticallyEnsureAdmin();

        var entries = string.IsNullOrWhiteSpace(match)
            ? _db.UserNameBlacklists
            : _db.UserNameBlacklists.Matching(match.Trim());

        await foreach (var item in entries.AsNoTracking().AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return new UserNameBlacklistDto {
                Id = item.Id,
                Value = item.Value,
                MatchType = item.MatchType,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
