using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using Testcontainers.PostgreSql;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Runs the PostgresNativeCleanup migration over limits that were stored out of range before the check
/// constraints existed, the way a long-lived production database has them.
/// </summary>
public sealed class LimitNormalizationMigrationTests
{
    private const string MigrationBefore = "20261120201415_MaterializeControlStats";

    // Stored cap -> cap after the migration. A cap outside the 16-bit range keeps its low 16 bits, which is what
    // it was read and enforced as all along, and anything that ends up under the 300 ms minimum is raised to it.
    private static readonly (int Stored, int Expected)[] DurationCases =
    [
        (5000, 5000),
        (65535, 65535),
        (65536, 300), // wraps to 0
        (70000, 4464),
        (100000, 34464),
        (-1, 65535),
        (100, 300)
    ];

    [Test]
    public async Task OutOfRangeLimits_AreWrappedOnceAndThenConstrained()
    {
        await using var container = new PostgreSqlBuilder(image: "timescale/timescaledb:latest-pg18").Build();
        await container.StartAsync();

        MigrationOpenShockContext CreateContext() => new(container.GetConnectionString(), false, NullLoggerFactory.Instance);

        var ownerId = Guid.CreateVersion7();
        var recipientId = Guid.CreateVersion7();
        var deviceId = Guid.CreateVersion7();
        var inviteId = Guid.CreateVersion7();
        var publicShareId = Guid.CreateVersion7();
        var shareCodeId = Guid.CreateVersion7();
        var wideRfShockerId = Guid.CreateVersion7();
        var wideDurationTokenId = Guid.CreateVersion7();
        var invertedDurationTokenId = Guid.CreateVersion7();
        var wideIntensityTokenId = Guid.CreateVersion7();
        var shockerIds = DurationCases.Select(_ => Guid.CreateVersion7()).ToArray();

        await using (var db = CreateContext())
        {
            await db.GetService<IMigrator>().MigrateAsync(MigrationBefore);

            // Everything is inserted in range and then pushed out of range with plain SQL, which is the only
            // way such values can exist: the model never held anything wider than the column's CLR type.
            db.Users.AddRange(NewUser(ownerId, "limitowner"), NewUser(recipientId, "limitrecipient"));
            db.Devices.Add(new Device { Id = deviceId, OwnerId = ownerId, Name = "Hub", Token = "limit-migration-token" });
            db.Shockers.AddRange(shockerIds.Append(wideRfShockerId).Select(id => new Shocker
            {
                Id = id,
                Name = "Shocker",
                Model = ShockerModelType.CaiXianlin,
                RfId = 1234,
                DeviceId = deviceId
            }));
            db.UserShares.AddRange(shockerIds.Select(id => new UserShare { SharedWithUserId = recipientId, ShockerId = id, AllowShock = true, AllowVibrate = true, AllowSound = true, AllowLiveControl = true, IsPaused = false, MaxDuration = 5000, MaxIntensity = 50 }));
            db.UserShareInvites.Add(new UserShareInvite { Id = inviteId, OwnerId = ownerId, RecipientUserId = recipientId });
            db.UserShareInviteShockers.Add(new UserShareInviteShocker { InviteId = inviteId, ShockerId = shockerIds[0], AllowShock = true, AllowVibrate = true, AllowSound = true, AllowLiveControl = true, IsPaused = false, MaxDuration = 5000, MaxIntensity = 50 });
            db.ShockerShareCodes.Add(new ShockerShareCode { Id = shareCodeId, ShockerId = shockerIds[0], AllowShock = true, AllowVibrate = true, AllowSound = true, AllowLiveControl = true, IsPaused = false, MaxDuration = 5000, MaxIntensity = 50 });
            db.PublicShares.Add(new PublicShare { Id = publicShareId, OwnerId = ownerId, Name = "Public" });
            db.PublicShareShockerMappings.Add(new PublicShareShocker { PublicShareId = publicShareId, ShockerId = shockerIds[0], AllowShock = true, AllowVibrate = true, AllowSound = true, AllowLiveControl = true, IsPaused = false, MaxDuration = 5000, MaxIntensity = 50 });
            db.ApiTokens.AddRange(
                NewToken(wideDurationTokenId, ownerId, "wide-duration"),
                NewToken(invertedDurationTokenId, ownerId, "inverted-duration"),
                NewToken(wideIntensityTokenId, ownerId, "wide-intensity"));
            await db.SaveChangesAsync();

            for (var i = 0; i < DurationCases.Length; i++)
            {
                await db.Database.ExecuteSqlAsync(
                    $"UPDATE user_shares SET max_duration = {DurationCases[i].Stored} WHERE shocker_id = {shockerIds[i]}");
            }

            await db.Database.ExecuteSqlAsync($"UPDATE user_shares SET max_intensity = 150 WHERE shocker_id = {shockerIds[0]}");
            await db.Database.ExecuteSqlAsync($"UPDATE user_share_invite_shockers SET max_duration = 100000, max_intensity = 150");
            await db.Database.ExecuteSqlAsync($"UPDATE shocker_share_codes SET max_duration = 100000, max_intensity = 150");
            await db.Database.ExecuteSqlAsync($"UPDATE public_share_shockers SET max_duration = 100000, max_intensity = 150, cooldown = -5");
            await db.Database.ExecuteSqlAsync($"UPDATE shockers SET rf_id = 70000 WHERE id = {wideRfShockerId}");
            await db.Database.ExecuteSqlAsync(
                $"UPDATE api_tokens SET shocker_control_duration_max = 100000 WHERE id = {wideDurationTokenId}");
            // The maximum wraps to 4464, which leaves the minimum above it
            await db.Database.ExecuteSqlAsync(
                $"UPDATE api_tokens SET shocker_control_duration_min = 5000, shocker_control_duration_max = 70000 WHERE id = {invertedDurationTokenId}");
            await db.Database.ExecuteSqlAsync(
                $"UPDATE api_tokens SET shocker_control_intensity_max = 150 WHERE id = {wideIntensityTokenId}");
        }

        await using (var db = CreateContext())
        {
            await db.Database.MigrateAsync();
        }

        await using (var db = CreateContext())
        {
            var userShares = await db.UserShares.AsNoTracking().ToDictionaryAsync(x => x.ShockerId);
            for (var i = 0; i < DurationCases.Length; i++)
            {
                await Assert.That((int?)userShares[shockerIds[i]].MaxDuration).IsEqualTo(DurationCases[i].Expected);
            }

            await Assert.That(userShares[shockerIds[0]].MaxIntensity).IsEqualTo((byte)100);

            var invite = await db.UserShareInviteShockers.AsNoTracking().SingleAsync();
            await Assert.That(invite.MaxDuration).IsEqualTo((ushort)34464);
            await Assert.That(invite.MaxIntensity).IsEqualTo((byte)100);

            var shareCode = await db.ShockerShareCodes.AsNoTracking().SingleAsync();
            await Assert.That(shareCode.MaxDuration).IsEqualTo((ushort)34464);
            await Assert.That(shareCode.MaxIntensity).IsEqualTo((byte)100);

            var publicShare = await db.PublicShareShockerMappings.AsNoTracking().SingleAsync();
            await Assert.That(publicShare.MaxDuration).IsEqualTo((ushort)34464);
            await Assert.That(publicShare.MaxIntensity).IsEqualTo((byte)100);
            await Assert.That(publicShare.Cooldown).IsEqualTo(0);

            var rfId = await db.Shockers.Where(x => x.Id == wideRfShockerId).Select(x => x.RfId).SingleAsync();
            await Assert.That(rfId).IsEqualTo((ushort)4464);

            var tokens = await db.ApiTokens.AsNoTracking().ToDictionaryAsync(x => x.Id);
            await Assert.That(tokens[wideDurationTokenId].ShockerControlDurationMax).IsEqualTo((ushort)34464);
            await Assert.That(tokens[invertedDurationTokenId].ShockerControlDurationMin).IsEqualTo((ushort)4464);
            await Assert.That(tokens[invertedDurationTokenId].ShockerControlDurationMax).IsEqualTo((ushort)4464);
            await Assert.That(tokens[wideIntensityTokenId].ShockerControlIntensityMax).IsEqualTo((byte)100);

            // From here on the database refuses the value instead of anything wrapping it
            var rejected = await Assert.That(() => db.Database.ExecuteSqlAsync(
                    $"UPDATE user_shares SET max_duration = 100000 WHERE shocker_id = {shockerIds[0]}"))
                .Throws<Npgsql.PostgresException>();
            await Assert.That(rejected!.SqlState).IsEqualTo(Npgsql.PostgresErrorCodes.CheckViolation);
        }
    }

    private static User NewUser(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        Email = $"{name}@test.org",
        PasswordHash = null,
        ActivatedAt = DateTime.UtcNow
    };

    private static ApiToken NewToken(Guid id, Guid userId, string name) => new()
    {
        Id = id,
        UserId = userId,
        Name = name,
        TokenHash = $"limit-migration-{name}",
        CreatedByIp = IPAddress.Loopback,
        Permissions = [PermissionType.Shockers_Use]
    };
}
