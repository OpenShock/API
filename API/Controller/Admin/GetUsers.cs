using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.Utils;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Gets all online devices
    /// </summary>
    /// <response code="200">All online devices</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("users")]
    [ProducesSuccess<IAsyncEnumerable<AdminUserResponse>>]
    public async Task<Paginated<AdminUserResponse>> GetUsers([FromQuery] [Range(0, 1000)] int limit = 100, [FromQuery] [Range(0, int.MaxValue)] int offset = 0)
    {
        var deferredCount = _db.Users.DeferredLongCount().FutureValue();

        var deferredUsers = _db.Users.AsNoTracking().OrderBy(x => x.CreatedAt).Skip(offset).Take(limit).Select(user =>
            new AdminUserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PasswordHashType = PasswordHashingUtils.GetPasswordHashingAlgorithm(user.PasswordHash),
                CreatedAt = user.CreatedAt,
                EmailActivated = user.EmailActived,
                Rank = user.Rank,
                Counts = new AdminUserCountsResponse
                {
                    ApiTokens = user.ApiTokens.Count,
                    Devices = user.Devices.Count,
                    Shockers = user.Devices.SelectMany(d => d.Shockers).Count(),
                    PasswordResetRequests = user.PasswordResets.Count,
                    ShockerControlLogs = user.ShockerControlLogs.Count,
                    ShockerShares = user.ShockerShares.Count,
                    ShockerShareLinks = user.ShockerSharesLinks.Count,
                    ChangeEmailRequests = user.UsersEmailChanges.Count,
                    ChangeNameRequests = user.UsersNameChanges.Count,
                    CreateUserRequests = user.UsersActivations.Count,
                }
            }).Future();

        return new Paginated<AdminUserResponse>
        {
            Data = await deferredUsers.ToListAsync(),
            Offset = offset,
            Limit = limit,
            Total = await deferredCount.ValueAsync(),
        };
    }

    public sealed class AdminUserResponse
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required string Email { get; init; }
        public required PasswordHashingAlgorithm PasswordHashType { get; set; }
        public required DateTime CreatedAt { get; init; }
        public required bool EmailActivated { get; init; }
        public required RankType Rank { get; init; }
        
        public required AdminUserCountsResponse Counts { get; init; }
    }

    public sealed class AdminUserCountsResponse
    {
        public required int Devices { get; init; }
        public required int Shockers { get; init; }
        public required int ApiTokens { get; init; }
        public required int PasswordResetRequests { get; init; }
        public required int ShockerControlLogs { get; init; }
        public required int ShockerShares { get; init; }
        public required int ShockerShareLinks { get; init; }
        public required int ChangeNameRequests { get; init; }
        public required int ChangeEmailRequests { get; init; }
        public required int CreateUserRequests { get; init; }
    }
}