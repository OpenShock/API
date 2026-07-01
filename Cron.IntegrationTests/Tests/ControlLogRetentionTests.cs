using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.Cron.IntegrationTests.Tests;

/// <summary>
/// End-to-end coverage for the Cron retention trim against real Postgres. Runs the cleanup job's *actual*
/// statement (<see cref="ShockerControlLogQueries.DeleteControlLogsBeyondPerUserLimitAsync"/>, the single
/// source of truth the job uses) over a seeded user -> device -> shocker -> logs graph, so the real table
/// and column names, the shocker -> device -> owner join, the per-user window function, and the newest-first
/// ordering are all exercised. A schema rename breaks this test rather than only the Cron host at runtime.
/// </summary>
public sealed class ControlLogRetentionTests
{
    [ClassDataSource<CronApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required CronApplicationFactory Factory { get; init; }

    [Test]
    public async Task DeleteControlLogsBeyondPerUserLimit_KeepsNewestPerUser_AndDeletesTheRest()
    {
        const int keep = 10;
        const int seed = 15; // 5 over the limit

        var userId = Guid.CreateVersion7();
        var deviceId = Guid.CreateVersion7();
        var shockerId = Guid.CreateVersion7();

        // Seed `seed` logs with strictly increasing timestamps so newest-first ordering is unambiguous.
        // Track the ids in age order; the newest `keep` should survive, the oldest `seed - keep` should go.
        var idsOldestToNewest = new List<Guid>(seed);
        var baseTime = DateTime.UtcNow - TimeSpan.FromDays(1);

        await using (var db = await Factory.DbContextFactory.CreateDbContextAsync())
        {
            db.Users.Add(new User
            {
                Id = userId,
                Name = $"logtrim{userId:N}"[..16],
                Email = $"logtrim-{userId:N}@test.org",
                SecurityStamp = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow
            });
            db.Devices.Add(new Device
            {
                Id = deviceId, OwnerId = userId, Name = "LogTrimHub",
                Token = CryptoUtils.RandomAlphaNumericString(256), CreatedAt = DateTime.UtcNow
            });
            db.Shockers.Add(new Shocker
            {
                Id = shockerId, Name = "LogTrimShocker", RfId = 1234,
                DeviceId = deviceId, Model = ShockerModelType.CaiXianlin
            });

            for (var i = 0; i < seed; i++)
            {
                var logId = Guid.CreateVersion7();
                idsOldestToNewest.Add(logId);
                db.ShockerControlLogs.Add(new ShockerControlLog
                {
                    Id = logId,
                    ShockerId = shockerId,
                    ControlledByUserId = userId,
                    Intensity = 50,
                    Duration = 1000,
                    Type = ControlType.Shock,
                    CustomName = null,
                    CreatedAt = baseTime + TimeSpan.FromMinutes(i)
                });
            }

            await db.SaveChangesAsync();
        }

        // Resolve the pooled OpenShockContext exactly as the Cron cleanup job does (DI-injected) so this
        // covers the same registration path that runs in production.
        await using var scope = Factory.Services.CreateAsyncScope();
        var db2 = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var deleted = await db2.DeleteControlLogsBeyondPerUserLimitAsync(keep);

        // The trim is global, but no other integration test writes shocker control logs, so this user's rows
        // are the only ones in the table: exactly the oldest `seed - keep` are deleted and the newest `keep`
        // remain. The count therefore equals this user's deletion.
        await Assert.That(deleted).IsEqualTo(seed - keep);

        var survivingIds = await db2.ShockerControlLogs
            .AsNoTracking()
            .Where(l => l.ShockerId == shockerId)
            .Select(l => l.Id)
            .ToListAsync();

        var expectedSurviving = idsOldestToNewest.Skip(seed - keep).ToHashSet();
        var expectedDeleted = idsOldestToNewest.Take(seed - keep).ToHashSet();

        await Assert.That(survivingIds.Count).IsEqualTo(keep);
        await Assert.That(survivingIds.ToHashSet().SetEquals(expectedSurviving)).IsTrue();
        await Assert.That(survivingIds.Any(id => expectedDeleted.Contains(id))).IsFalse();
    }
}
