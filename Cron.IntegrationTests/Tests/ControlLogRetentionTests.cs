using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using OpenShock.Internal.Common.Utils;

namespace OpenShock.Cron.IntegrationTests.Tests;

/// <summary>
/// End-to-end coverage for the Cron retention trim against real Postgres. Runs the cleanup job's *actual*
/// statement (<see cref="ShockerControlLogQueries.DeleteControlLogsBeyondPerUserLimitAsync"/>, the single
/// source of truth the job uses) over a seeded graph, so the real table/column names, the
/// shocker -> device -> owner join, the per-user window function, and newest-first ordering are exercised.
/// A schema rename breaks this test rather than only the Cron host at runtime.
/// </summary>
public sealed class ControlLogRetentionTests
{
    [ClassDataSource<CronApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required CronApplicationFactory Factory { get; init; }

    [Test]
    public async Task DeleteControlLogsBeyondPerUserLimit_TrimsPerOwner_KeepsNewest_AndSparesOwnersUnderTheLimit()
    {
        const int keep = 10;

        // Owner A is over the limit; owner B is under it. A's logs are attributed to two *different*
        // controller users (each under the limit on its own) and B's logs to B itself - so the trim is only
        // correct if it partitions by the shocker's OWNER (d.owner_id). If it instead partitioned by the
        // controller (l.controlled_by_user_id) it would delete nothing (no controller exceeds the limit); if
        // it dropped PARTITION BY entirely it would rank globally and wrongly delete B's (older) logs too.
        var ownerA = Guid.CreateVersion7();
        var ownerB = Guid.CreateVersion7();
        var controllerC = Guid.CreateVersion7();
        var controllerD = Guid.CreateVersion7();

        var shockerA = Guid.CreateVersion7();
        var shockerB = Guid.CreateVersion7();

        var baseTime = DateTime.UtcNow - TimeSpan.FromDays(1);

        // A: 15 logs (5 over). Newest 10 survive, oldest 5 go. Split across controllers C (first 8) and D.
        var aOldestToNewest = new List<Guid>(15);
        // B: 5 logs, all older than A's - a global (unpartitioned) trim would wrongly delete them.
        var bIds = new List<Guid>(5);

        await using (var db = await Factory.DbContextFactory.CreateDbContextAsync())
        {
            db.Users.Add(NewUser(ownerA, "own-a"));
            db.Users.Add(NewUser(ownerB, "own-b"));
            db.Users.Add(NewUser(controllerC, "ctl-c"));
            db.Users.Add(NewUser(controllerD, "ctl-d"));

            AddShocker(db, shockerB, ownerB, rfId: 2000);
            AddShocker(db, shockerA, ownerA, rfId: 1000);

            // B's logs sit at minutes 0..4 (oldest overall) so a global newest-10 trim would drop them.
            for (var i = 0; i < 5; i++)
            {
                var logId = Guid.CreateVersion7();
                bIds.Add(logId);
                db.ShockerControlLogs.Add(NewLog(logId, shockerB, ownerB, baseTime + TimeSpan.FromMinutes(i)));
            }

            // A's logs sit at minutes 100..114 (newest overall), the first 8 by C and the last 7 by D.
            for (var i = 0; i < 15; i++)
            {
                var logId = Guid.CreateVersion7();
                aOldestToNewest.Add(logId);
                var controller = i < 8 ? controllerC : controllerD;
                db.ShockerControlLogs.Add(NewLog(logId, shockerA, controller, baseTime + TimeSpan.FromMinutes(100 + i)));
            }

            await db.SaveChangesAsync();
        }

        // Resolve the pooled OpenShockContext exactly as the Cron cleanup job does (DI-injected) so this
        // covers the same registration path that runs in production.
        await using var scope = Factory.Services.CreateAsyncScope();
        var db2 = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var deleted = await db2.DeleteControlLogsBeyondPerUserLimitAsync(keep);

        // Only owner A is over the limit, so exactly its oldest 5 are removed. No other integration test
        // writes control logs, so the global count equals A's deletion: 5. (Dropping PARTITION BY would
        // delete 10; partitioning by controller instead of owner would delete 0.)
        await Assert.That(deleted).IsEqualTo(5);

        var survivingA = (await db2.ShockerControlLogs.AsNoTracking()
            .Where(l => l.ShockerId == shockerA).Select(l => l.Id).ToListAsync()).ToHashSet();
        var survivingB = await db2.ShockerControlLogs.AsNoTracking()
            .Where(l => l.ShockerId == shockerB).Select(l => l.Id).ToListAsync();

        // A keeps its newest 10; its oldest 5 are gone.
        await Assert.That(survivingA.SetEquals(aOldestToNewest.Skip(5).ToHashSet())).IsTrue();
        await Assert.That(aOldestToNewest.Take(5).Any(survivingA.Contains)).IsFalse();

        // B is under the limit, so every one of its (older) logs survives - a global trim would have deleted them.
        await Assert.That(survivingB.Count).IsEqualTo(5);
    }

    private static User NewUser(Guid id, string prefix) => new()
    {
        Id = id,
        Name = $"{prefix}{id:N}"[..16],
        Email = $"{prefix}-{id:N}@test.org",
        SecurityStamp = Guid.CreateVersion7(),
        CreatedAt = DateTime.UtcNow,
        ActivatedAt = DateTime.UtcNow
    };

    private static void AddShocker(OpenShockContext db, Guid shockerId, Guid ownerId, ushort rfId)
    {
        var deviceId = Guid.CreateVersion7();
        db.Devices.Add(new Device
        {
            Id = deviceId, OwnerId = ownerId, Name = "LogTrimHub",
            Token = CryptoUtils.RandomString(256), CreatedAt = DateTime.UtcNow
        });
        db.Shockers.Add(new Shocker
        {
            Id = shockerId, Name = "LogTrimShocker", RfId = rfId,
            DeviceId = deviceId, Model = ShockerModelType.CaiXianlin
        });
    }

    private static ShockerControlLog NewLog(Guid id, Guid shockerId, Guid controllerId, DateTime createdAt) => new()
    {
        Id = id,
        ShockerId = shockerId,
        ControlledByUserId = controllerId,
        Intensity = 50,
        Duration = 1000,
        Type = ControlType.Shock,
        CustomName = null,
        CreatedAt = createdAt
    };
}
