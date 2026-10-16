using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class ControlStatsTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task SendControl_RecordsStats_ThatSurviveLogDeletion()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("stats"),
            TestHelper.UniqueEmail("stats"), "SecurePassword123#");
        var shockerId = await CreateShocker(user.Id, "Stats Shocker");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        foreach (var (type, intensity, duration) in new[] { ("Shock", 50, 1000), ("Shock", 100, 2000), ("Vibrate", 30, 500), ("Stop", 1, 300) })
        {
            var response = await client.PostAsync("/2/shockers/control", TestHelper.JsonContent(new
            {
                shocks = new[] { new { id = shockerId, type, intensity, duration } }
            }));
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        // The rollup must not depend on the logs, which get trimmed
        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await db.ShockerControlLogs.Where(x => x.ShockerId == shockerId).ExecuteDeleteAsync();
        }

        using var doc = await GetStats(client);
        var received = doc.RootElement.GetProperty("received");

        var totals = received.GetProperty("totals");
        await Assert.That(totals.GetArrayLength()).IsEqualTo(2); // Stop is not counted

        var shock = FindTotals(totals, "Shock", live: false);
        await Assert.That(shock.GetProperty("count").GetInt64()).IsEqualTo(2);
        await Assert.That(shock.GetProperty("durationMs").GetInt64()).IsEqualTo(3000);
        await Assert.That(shock.GetProperty("averageIntensity").GetDouble()).IsEqualTo((50.0 * 1000 + 100.0 * 2000) / 3000).Within(0.001);
        await Assert.That(shock.GetProperty("maxIntensity").GetInt32()).IsEqualTo(100);

        var vibrate = FindTotals(totals, "Vibrate", live: false);
        await Assert.That(vibrate.GetProperty("count").GetInt64()).IsEqualTo(1);

        var shocker = received.GetProperty("shockers").EnumerateArray().Single();
        await Assert.That(shocker.GetProperty("shockerId").GetGuid()).IsEqualTo(shockerId);
        await Assert.That(shocker.GetProperty("shockerName").GetString()).IsEqualTo("Stats Shocker");

        var controller = received.GetProperty("controllers").EnumerateArray().Single();
        await Assert.That(controller.GetProperty("userId").GetGuid()).IsEqualTo(user.Id);
        await Assert.That(controller.GetProperty("user").GetProperty("name").GetString()).IsEqualTo(user.Username);

        await Assert.That(received.GetProperty("days").GetArrayLength()).IsEqualTo(1);

        // Controlling your own shocker counts as given too
        var given = doc.RootElement.GetProperty("given");
        await Assert.That(FindTotals(given.GetProperty("totals"), "Shock", live: false).GetProperty("count").GetInt64()).IsEqualTo(2);
        await Assert.That(given.GetProperty("owners").EnumerateArray().Single().GetProperty("userId").GetGuid()).IsEqualTo(user.Id);
    }

    [Test]
    public async Task DeletingShockers_KeepsTheirStats_WithoutUniqueConflicts()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("statsdel"),
            TestHelper.UniqueEmail("statsdel"), "SecurePassword123#");
        var shockerA = await CreateShocker(user.Id, "A");
        var shockerB = await CreateShocker(user.Id, "B");
        var bucket = new DateTime(2026, 3, 1, 12, 15, 0, DateTimeKind.Utc);

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

            // Identical keys apart from the shocker, so after SET NULL they would collide on a full unique index
            await db.UpsertControlStatsAsync([
                Delta(user.Id, shockerA, user.Id, bucket, count: 2),
                Delta(user.Id, shockerB, user.Id, bucket, count: 3)
            ]);

            await db.Shockers.Where(x => x.Id == shockerA).ExecuteDeleteAsync();
            await db.Shockers.Where(x => x.Id == shockerB).ExecuteDeleteAsync();

            await Assert.That(await db.ControlStats.CountAsync(x => x.OwnerUserId == user.Id && x.ShockerId == null)).IsEqualTo(2);
        }

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        using var doc = await GetStats(client);

        var deleted = doc.RootElement.GetProperty("received").GetProperty("shockers").EnumerateArray().Single();
        await Assert.That(deleted.GetProperty("shockerId").ValueKind).IsEqualTo(JsonValueKind.Null);
        await Assert.That(FindTotals(deleted.GetProperty("stats"), "Shock", live: false).GetProperty("count").GetInt64()).IsEqualTo(5);
    }

    [Test]
    public async Task Upsert_MergesDuplicateKeysAndAccumulates()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("statsmerge"),
            TestHelper.UniqueEmail("statsmerge"), "SecurePassword123#");
        var shockerId = await CreateShocker(userId, "Merge");
        var bucket = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        // Same bucket, same key twice in one batch, plus a guest (null controller) bucket
        await db.UpsertControlStatsAsync([
            Delta(userId, shockerId, userId, bucket.AddMinutes(5), count: 1, intensity: 20),
            Delta(userId, shockerId, userId, bucket.AddMinutes(14), count: 1, intensity: 80),
            Delta(userId, shockerId, null, bucket, count: 4)
        ]);
        await db.UpsertControlStatsAsync([Delta(userId, shockerId, userId, bucket, count: 1, intensity: 10)]);
        await db.UpsertControlStatsAsync([Delta(userId, shockerId, null, bucket, count: 1)]);

        var rows = await db.ControlStats.AsNoTracking().Where(x => x.ShockerId == shockerId).ToArrayAsync();
        await Assert.That(rows.Length).IsEqualTo(2);

        var own = rows.Single(x => x.ControllerUserId == userId);
        await Assert.That(own.OwnerUserId).IsEqualTo(userId);
        await Assert.That(own.Bucket).IsEqualTo(bucket);
        await Assert.That(own.Count).IsEqualTo(3);
        await Assert.That(own.DurationMs).IsEqualTo(3000);
        await Assert.That(own.MaxIntensity).IsEqualTo((byte)80);

        await Assert.That(rows.Single(x => x.ControllerUserId == null).Count).IsEqualTo(5);
    }

    [Test]
    public async Task Upsert_ForShockerDeletedBeforeFlush_KeepsDeltaWithoutShocker()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("statsgone"),
            TestHelper.UniqueEmail("statsgone"), "SecurePassword123#");
        var shockerId = await CreateShocker(userId, "Gone");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        // Buffered live control activity whose shocker got deleted before the flush
        await db.Shockers.Where(x => x.Id == shockerId).ExecuteDeleteAsync();
        await db.UpsertControlStatsAsync([
            Delta(userId, shockerId, userId, new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc), count: 3)
        ]);

        var row = await db.ControlStats.AsNoTracking().SingleAsync(x => x.OwnerUserId == userId);
        await Assert.That(row.ShockerId).IsNull();
        await Assert.That(row.Count).IsEqualTo(3);
    }

    [Test]
    public async Task ApplyBatch_RetriedWithSameId_IsCountedOnce()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("statsbatch"),
            TestHelper.UniqueEmail("statsbatch"), "SecurePassword123#");
        var shockerId = await CreateShocker(userId, "Batch");
        var batchId = Guid.CreateVersion7();
        ControlStatDelta[] deltas = [Delta(userId, shockerId, userId, new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc), count: 2)];

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var createdAt = DateTime.UtcNow;
        await Assert.That(await db.ApplyControlStatBatchAsync(batchId, createdAt, deltas)).IsEqualTo(ControlStatBatchResult.Applied);
        // Retry after a commit whose acknowledgement got lost
        await Assert.That(await db.ApplyControlStatBatchAsync(batchId, createdAt, deltas)).IsEqualTo(ControlStatBatchResult.AlreadyApplied);

        var row = await db.ControlStats.AsNoTracking().SingleAsync(x => x.ShockerId == shockerId);
        await Assert.That(row.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ApplyBatch_OlderThanReplayLifetime_IsRefused()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("statsexp"),
            TestHelper.UniqueEmail("statsexp"), "SecurePassword123#");
        var shockerId = await CreateShocker(userId, "Expired");
        var createdAt = DateTime.UtcNow - ControlStatQueries.BatchReplayLifetime - TimeSpan.FromMinutes(1);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        // Its marker may already be pruned, so applying it could count it a second time
        var result = await db.ApplyControlStatBatchAsync(Guid.CreateVersion7(), createdAt,
            [Delta(userId, shockerId, userId, createdAt, count: 1)]);

        await Assert.That(result).IsEqualTo(ControlStatBatchResult.Expired);
        await Assert.That(await db.ControlStats.AnyAsync(x => x.ShockerId == shockerId)).IsFalse();
    }

    [Test]
    public async Task Stats_GroupsDaysInRequestedTimeZone_AndFiltersRange()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("statstz"),
            TestHelper.UniqueEmail("statstz"), "SecurePassword123#");
        var shockerId = await CreateShocker(user.Id, "Tz");

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await db.UpsertControlStatsAsync([
                // 23:00 UTC on Jan 1 is already Jan 2 in Berlin (UTC+1)
                Delta(user.Id, shockerId, user.Id, new DateTime(2026, 1, 1, 23, 0, 0, DateTimeKind.Utc), count: 1),
                Delta(user.Id, shockerId, user.Id, new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc), count: 1)
            ]);
        }

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        using (var utc = await GetStats(client))
            await Assert.That(Days(utc)).IsEquivalentTo(new[] { "2026-01-01", "2026-01-05" });

        using (var berlin = await GetStats(client, "?tz=Europe/Berlin"))
            await Assert.That(Days(berlin)).IsEquivalentTo(new[] { "2026-01-02", "2026-01-05" });

        using (var ranged = await GetStats(client, "?tz=Europe/Berlin&from=2026-01-02&to=2026-01-02"))
            await Assert.That(Days(ranged)).IsEquivalentTo(new[] { "2026-01-02" });
    }

    [Test]
    public async Task Stats_HalfHourOffsetTimeZone_SplitsDaysAtLocalMidnight()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("statskol"),
            TestHelper.UniqueEmail("statskol"), "SecurePassword123#");
        var shockerId = await CreateShocker(user.Id, "Kolkata");

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await db.UpsertControlStatsAsync([
                // Midnight in Kolkata (UTC+5:30) is 18:30 UTC, in the middle of an hour
                Delta(user.Id, shockerId, user.Id, new DateTime(2026, 1, 1, 18, 15, 0, DateTimeKind.Utc), count: 1),
                Delta(user.Id, shockerId, user.Id, new DateTime(2026, 1, 1, 18, 45, 0, DateTimeKind.Utc), count: 2)
            ]);
        }

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        using (var all = await GetStats(client, "?tz=Asia/Kolkata"))
            await Assert.That(Days(all)).IsEquivalentTo(new[] { "2026-01-01", "2026-01-02" });

        using var secondDay = await GetStats(client, "?tz=Asia/Kolkata&from=2026-01-02");
        await Assert.That(Days(secondDay)).IsEquivalentTo(new[] { "2026-01-02" });
        await Assert.That(FindTotals(secondDay.RootElement.GetProperty("received").GetProperty("totals"), "Shock", live: false)
            .GetProperty("count").GetInt64()).IsEqualTo(2);
    }

    [Test]
    public async Task Stats_InvalidTimeZoneOrRange_Returns400()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("statsbad"),
            TestHelper.UniqueEmail("statsbad"), "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var badTz = await client.GetAsync("/1/users/self/control-stats?tz=Not/AZone");
        await Assert.That(badTz.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var badRange = await client.GetAsync("/1/users/self/control-stats?from=2026-02-01&to=2026-01-01");
        await Assert.That(badRange.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Stats_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/1/users/self/control-stats");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private static ControlStatDelta Delta(Guid ownerId, Guid shockerId, Guid? controllerId, DateTime at, long count, byte intensity = 50) =>
        new(ownerId, shockerId, controllerId, at, ControlType.Shock, Live: false, count, DurationMs: count * 1000,
            IntensityMs: count * 1000 * intensity, intensity);

    private async Task<Guid> CreateShocker(Guid ownerId, string name)
    {
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, ownerId);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var shockerId = Guid.CreateVersion7();
        db.Shockers.Add(new Shocker
        {
            Id = shockerId,
            Name = name,
            Model = ShockerModelType.CaiXianlin,
            RfId = 1234,
            DeviceId = deviceId
        });
        await db.SaveChangesAsync();
        return shockerId;
    }

    private static async Task<JsonDocument> GetStats(HttpClient client, string query = "")
    {
        var response = await client.GetAsync("/1/users/self/control-stats" + query);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static JsonElement FindTotals(JsonElement totals, string type, bool live) => totals.EnumerateArray()
        .Single(x => x.GetProperty("type").GetString() == type && x.GetProperty("live").GetBoolean() == live);

    private static string[] Days(JsonDocument doc) => doc.RootElement.GetProperty("received").GetProperty("days")
        .EnumerateArray().Select(x => x.GetProperty("date").GetString()!).ToArray();
}
