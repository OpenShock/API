using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.OpenShockDb;
using static OpenShock.API.IntegrationTests.Helpers.ControlLogHelper;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class ControlStatsTests
{
    private const string Password = "SecurePassword123#";

    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task SendControl_IsCountedInStats_RightAway()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("stats"),
            TestHelper.UniqueEmail("stats"), Password);
        var shockerId = await CreateShocker(WebApplicationFactory, user.Id, "Stats Shocker");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        foreach (var (type, intensity, duration) in new[]
                     { ("Shock", 50, 1000), ("Shock", 100, 2000), ("Vibrate", 30, 500), ("Stop", 1, 300) })
        {
            var response = await client.PostAsync("/2/shockers/control", TestHelper.JsonContent(new
            {
                shocks = new[] { new { id = shockerId, type, intensity, duration } }
            }));
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        using var doc = await GetStats(client);
        var received = doc.RootElement.GetProperty("received");

        var totals = received.GetProperty("totals");
        await Assert.That(totals.GetArrayLength()).IsEqualTo(2); // Stop is not counted

        var shock = FindTotals(totals, "Shock", live: false);
        await Assert.That(shock.GetProperty("count").GetInt64()).IsEqualTo(2);
        await Assert.That(shock.GetProperty("durationMs").GetInt64()).IsEqualTo(3000);
        await Assert.That(shock.GetProperty("averageIntensity").GetDouble())
            .IsEqualTo((50.0 * 1000 + 100.0 * 2000) / 3000).Within(0.001);
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
        await Assert.That(FindTotals(given.GetProperty("totals"), "Shock", live: false).GetProperty("count").GetInt64())
            .IsEqualTo(2);
        await Assert.That(given.GetProperty("owners").EnumerateArray().Single().GetProperty("userId").GetGuid())
            .IsEqualTo(user.Id);
    }

    [Test]
    public async Task LiveControlBursts_CountOnce_WithWeightedAverageAndPeakIntensity()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("statslive"),
            TestHelper.UniqueEmail("statslive"), Password);
        var shockerId = await CreateShocker(WebApplicationFactory, user.Id, "Live");
        var now = DateTime.UtcNow;

        await Insert(WebApplicationFactory,
            Log(user.Id, shockerId, user.Id, now, intensity: 30, duration: 2000, live: true, maxIntensity: 80),
            Log(user.Id, shockerId, user.Id, now, intensity: 60, duration: 1000, live: true));

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        using var doc = await GetStats(client);

        var live = FindTotals(doc.RootElement.GetProperty("received").GetProperty("totals"), "Shock", live: true);
        await Assert.That(live.GetProperty("count").GetInt64()).IsEqualTo(2);
        await Assert.That(live.GetProperty("durationMs").GetInt64()).IsEqualTo(3000);
        await Assert.That(live.GetProperty("averageIntensity").GetDouble())
            .IsEqualTo((30.0 * 2000 + 60.0 * 1000) / 3000).Within(0.001);
        await Assert.That(live.GetProperty("maxIntensity").GetInt32()).IsEqualTo(80);
    }

    [Test]
    public async Task Insert_RetriedWithSameIds_IsCountedOnce()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("statsretry"),
            TestHelper.UniqueEmail("statsretry"), Password);
        var shockerId = await CreateShocker(WebApplicationFactory, userId, "Retry");
        ShockerControlLog[] logs = [Log(userId, shockerId, userId, DateTime.UtcNow, live: true)];

        await Assert.That(await Insert(WebApplicationFactory, logs)).IsEqualTo(1);
        // Retry after a commit whose acknowledgement got lost
        await Assert.That(await Insert(WebApplicationFactory, logs)).IsEqualTo(0);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.ControlStats.Where(x => x.ShockerId == shockerId).SumAsync(x => x.Count))
            .IsEqualTo(1);
    }

    [Test]
    public async Task Insert_ForOwnerDeletedBeforeFlush_IsDropped()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("statsgone"),
            TestHelper.UniqueEmail("statsgone"), Password);
        var shockerId = await CreateShocker(WebApplicationFactory, userId, "Gone");

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await db.Users.Where(x => x.Id == userId).ExecuteDeleteAsync();
        }

        // Buffered live control whose owner deleted their account before the flush
        await Assert
            .That(await Insert(WebApplicationFactory, Log(userId, shockerId, userId, DateTime.UtcNow, live: true)))
            .IsEqualTo(0);
    }

    [Test]
    public async Task DeletingShockers_KeepsTheirStats_CombinedIntoOneEntry()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("statsdel"),
            TestHelper.UniqueEmail("statsdel"), Password);
        var shockerA = await CreateShocker(WebApplicationFactory, user.Id, "A");
        var shockerB = await CreateShocker(WebApplicationFactory, user.Id, "B");
        var kept = await CreateShocker(WebApplicationFactory, user.Id, "Kept");
        var now = DateTime.UtcNow;

        await Insert(WebApplicationFactory,
            Log(user.Id, shockerA, user.Id, now), Log(user.Id, shockerA, user.Id, now),
            Log(user.Id, shockerB, user.Id, now), Log(user.Id, shockerB, user.Id, now),
            Log(user.Id, shockerB, user.Id, now),
            Log(user.Id, kept, user.Id, now));

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await db.Shockers.Where(x => x.Id == shockerA || x.Id == shockerB).ExecuteDeleteAsync();
        }

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);
        using var doc = await GetStats(client);

        var shockers = doc.RootElement.GetProperty("received").GetProperty("shockers").EnumerateArray().ToArray();
        await Assert.That(shockers.Length).IsEqualTo(2);

        var deleted = shockers.Single(x => x.GetProperty("shockerId").ValueKind == JsonValueKind.Null);
        await Assert.That(deleted.GetProperty("shockerName").ValueKind).IsEqualTo(JsonValueKind.Null);
        await Assert
            .That(FindTotals(deleted.GetProperty("stats"), "Shock", live: false).GetProperty("count").GetInt64())
            .IsEqualTo(5);

        var remaining = shockers.Single(x => x.GetProperty("shockerId").ValueKind != JsonValueKind.Null);
        await Assert.That(remaining.GetProperty("shockerName").GetString()).IsEqualTo("Kept");
    }

    [Test]
    public async Task DeletingAController_KeepsTheirStats()
    {
        var owner = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("statsctlown"),
            TestHelper.UniqueEmail("statsctlown"), Password);
        var controllerId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("statsctl"),
            TestHelper.UniqueEmail("statsctl"), Password);
        var shockerId = await CreateShocker(WebApplicationFactory, owner.Id, "Controlled");

        await Insert(WebApplicationFactory, Log(owner.Id, shockerId, controllerId, DateTime.UtcNow));

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await db.Users.Where(x => x.Id == controllerId).ExecuteDeleteAsync();
        }

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, owner.SessionToken);
        using var doc = await GetStats(client);

        var controller = doc.RootElement.GetProperty("received").GetProperty("controllers").EnumerateArray().Single();
        await Assert.That(controller.GetProperty("userId").GetGuid()).IsEqualTo(controllerId);
        await Assert.That(controller.GetProperty("user").ValueKind).IsEqualTo(JsonValueKind.Null);
        await Assert
            .That(FindTotals(controller.GetProperty("stats"), "Shock", live: false).GetProperty("count").GetInt64())
            .IsEqualTo(1);
    }

    [Test]
    public async Task DeletingTheOwner_DeletesTheirLogs_AndTheirStatsForControllers()
    {
        var ownerId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("statsownd"),
            TestHelper.UniqueEmail("statsownd"), Password);
        var controller = await TestHelper.CreateAndLoginUser(WebApplicationFactory,
            TestHelper.UniqueUsername("statsownctl"),
            TestHelper.UniqueEmail("statsownctl"), Password);
        var shockerId = await CreateShocker(WebApplicationFactory, ownerId, "Owned");

        await Insert(WebApplicationFactory,
            Log(ownerId, shockerId, controller.Id, new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc)),
            Log(ownerId, shockerId, controller.Id, DateTime.UtcNow));
        await RefreshControlStats(WebApplicationFactory);

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await db.Users.Where(x => x.Id == ownerId).ExecuteDeleteAsync();
            await Assert.That(await db.ShockerControlLogs.AnyAsync(x => x.OwnerUserId == ownerId)).IsFalse();
        }

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, controller.SessionToken);
        using var doc = await GetStats(client);

        var given = doc.RootElement.GetProperty("given");
        await Assert.That(given.GetProperty("totals").GetArrayLength()).IsEqualTo(0);
        await Assert.That(given.GetProperty("owners").GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task Stats_GroupsDaysInRequestedTimeZone_AndFiltersRange()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("statstz"),
            TestHelper.UniqueEmail("statstz"), Password);
        var shockerId = await CreateShocker(WebApplicationFactory, user.Id, "Tz");

        await Insert(WebApplicationFactory,
            // 23:00 UTC on Jan 1 is already Jan 2 in Berlin (UTC+1)
            Log(user.Id, shockerId, user.Id, new DateTime(2026, 1, 1, 23, 0, 0, DateTimeKind.Utc)),
            Log(user.Id, shockerId, user.Id, new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc)));
        await RefreshControlStats(WebApplicationFactory);

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
            TestHelper.UniqueEmail("statskol"), Password);
        var shockerId = await CreateShocker(WebApplicationFactory, user.Id, "Kolkata");

        await Insert(WebApplicationFactory,
            // Midnight in Kolkata (UTC+5:30) is 18:30 UTC, in the middle of an hour
            Log(user.Id, shockerId, user.Id, new DateTime(2026, 1, 1, 18, 15, 0, DateTimeKind.Utc)),
            Log(user.Id, shockerId, user.Id, new DateTime(2026, 1, 1, 18, 45, 0, DateTimeKind.Utc)),
            Log(user.Id, shockerId, user.Id, new DateTime(2026, 1, 1, 18, 50, 0, DateTimeKind.Utc)));
        await RefreshControlStats(WebApplicationFactory);

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        using (var all = await GetStats(client, "?tz=Asia/Kolkata"))
            await Assert.That(Days(all)).IsEquivalentTo(new[] { "2026-01-01", "2026-01-02" });

        using var secondDay = await GetStats(client, "?tz=Asia/Kolkata&from=2026-01-02");
        await Assert.That(Days(secondDay)).IsEquivalentTo(new[] { "2026-01-02" });
        await Assert.That(FindTotals(secondDay.RootElement.GetProperty("received").GetProperty("totals"), "Shock",
                live: false)
            .GetProperty("count").GetInt64()).IsEqualTo(2);
    }

    [Test]
    public async Task Stats_InvalidTimeZoneOrRange_Returns400()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername("statsbad"),
            TestHelper.UniqueEmail("statsbad"), Password);
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