using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.AutomationTokens;
using OpenShock.Common.Utils;
using static OpenShock.API.IntegrationTests.Helpers.ControlLogHelper;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class ControlStatChangesTests
{
    private const string Password = "SecurePassword123#";
    private const string Endpoint = "/1/integrations/control-stats/changes";

    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task WithoutControlStatsReadToken_Returns403()
    {
        var body = TestHelper.JsonContent(new { userIds = new[] { Guid.NewGuid() } });

        using var anonymous = WebApplicationFactory.CreateClient();
        await Assert.That((await anonymous.PostAsync(Endpoint, body)).StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        using var wrongType = await CreateTokenClient([AutomationTokenType.Turnstile, AutomationTokenType.RateLimit]);
        await Assert.That((await wrongType.PostAsync(Endpoint, body)).StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task InitialSync_ReportsEveryRequestedUser_WithReceivedAndGivenTotals()
    {
        var owner = await CreateUser("chgowner");
        var controller = await CreateUser("chgctrl");
        var idle = await CreateUser("chgidle");
        var notRequested = await CreateUser("chgother");
        var shockerId = await CreateShocker(WebApplicationFactory, owner, "Changes");
        var at = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        await Insert(WebApplicationFactory,
        [
            Log(owner, shockerId, controller, at, intensity: 40),
            Log(owner, shockerId, controller, at, intensity: 40),
            .. Enumerable.Range(0, 7).Select(_ => Log(owner, shockerId, notRequested, at))
        ]);
        await RefreshControlStats(WebApplicationFactory);

        using var client = await CreateTokenClient([AutomationTokenType.ControlStatsRead]);
        using var doc = await Post(client, new { userIds = new[] { owner, controller, idle } });

        var users = doc.RootElement.GetProperty("users").EnumerateArray()
            .ToDictionary(x => x.GetProperty("userId").GetGuid());
        await Assert.That(users.Keys).IsEquivalentTo(new[] { owner, controller, idle });

        // Lifetime totals, so the not requested controller's activity counts towards the owner as well
        var received = SingleShock(users[owner].GetProperty("received"));
        await Assert.That(received.GetProperty("count").GetInt64()).IsEqualTo(9);
        await Assert.That(users[owner].GetProperty("given").GetArrayLength()).IsEqualTo(0);

        var given = SingleShock(users[controller].GetProperty("given"));
        await Assert.That(given.GetProperty("count").GetInt64()).IsEqualTo(2);
        await Assert.That(given.GetProperty("averageIntensity").GetDouble()).IsEqualTo(40).Within(0.001);

        await Assert.That(users[idle].GetProperty("received").GetArrayLength()).IsEqualTo(0);
        await Assert.That(users[idle].GetProperty("given").GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task Since_OnlyReportsUsersWithRecentActivity()
    {
        var owner = await CreateUser("chgsince");
        var quiet = await CreateUser("chgquiet");
        var shockerId = await CreateShocker(WebApplicationFactory, owner, "Since");
        var quietShockerId = await CreateShocker(WebApplicationFactory, quiet, "Quiet");

        // Old activity only, outside of any lookback
        var old = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await Insert(WebApplicationFactory,
            Log(owner, shockerId, owner, old), Log(owner, shockerId, owner, old), Log(owner, shockerId, owner, old),
            Log(quiet, quietShockerId, quiet, old));
        await Backdate(owner, quiet);
        await RefreshControlStats(WebApplicationFactory);

        using var client = await CreateTokenClient([AutomationTokenType.ControlStatsRead]);
        var userIds = new[] { owner, quiet };

        DateTime cursor;
        using (var initial = await Post(client, new { userIds }))
            cursor = initial.RootElement.GetProperty("cursor").GetDateTime();

        using (var unchanged = await Post(client, new { since = cursor, userIds }))
            await Assert.That(unchanged.RootElement.GetProperty("users").GetArrayLength()).IsEqualTo(0);

        await Insert(WebApplicationFactory, Log(owner, shockerId, owner, DateTime.UtcNow));

        using var changed = await Post(client, new { since = cursor, userIds });
        var user = changed.RootElement.GetProperty("users").EnumerateArray().Single();
        await Assert.That(user.GetProperty("userId").GetGuid()).IsEqualTo(owner);
        await Assert.That(SingleShock(user.GetProperty("received")).GetProperty("count").GetInt64()).IsEqualTo(4);
        await Assert.That(SingleShock(user.GetProperty("given")).GetProperty("count").GetInt64()).IsEqualTo(4);
    }

    [Test]
    public async Task Since_AtMinValue_ReportsUsersWithAnyActivity()
    {
        var owner = await CreateUser("chgmin");
        var idle = await CreateUser("chgminidle");
        var shockerId = await CreateShocker(WebApplicationFactory, owner, "MinValue");
        var at = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await Insert(WebApplicationFactory, Log(owner, shockerId, owner, at), Log(owner, shockerId, owner, at));
        await RefreshControlStats(WebApplicationFactory);

        using var client = await CreateTokenClient([AutomationTokenType.ControlStatsRead]);

        // With and without offset, the latter is read as UTC
        foreach (var since in new[] { "0001-01-01T00:00:00Z", "0001-01-01T00:00:00" })
        {
            using var doc = await Post(client, new { since, userIds = new[] { owner, idle } });

            var user = doc.RootElement.GetProperty("users").EnumerateArray().Single();
            await Assert.That(user.GetProperty("userId").GetGuid()).IsEqualTo(owner);
            await Assert.That(SingleShock(user.GetProperty("received")).GetProperty("count").GetInt64()).IsEqualTo(2);
        }
    }

    [Test]
    public async Task Since_ReportsLateWrites()
    {
        var owner = await CreateUser("chglate");
        var shockerId = await CreateShocker(WebApplicationFactory, owner, "Late");

        using var client = await CreateTokenClient([AutomationTokenType.ControlStatsRead]);
        var userIds = new[] { owner };

        DateTime cursor;
        using (var initial = await Post(client, new { userIds }))
            cursor = initial.RootElement.GetProperty("cursor").GetDateTime();

        // Live control from hours ago that only reached the database now, e.g. after an outage
        await Insert(WebApplicationFactory, Log(owner, shockerId, owner, DateTime.UtcNow.AddHours(-3), live: true));

        using var changed = await Post(client, new { since = cursor, userIds });
        var user = changed.RootElement.GetProperty("users").EnumerateArray().Single();
        await Assert.That(user.GetProperty("userId").GetGuid()).IsEqualTo(owner);
    }

    [Test]
    public async Task DeletedOwners_AreLeftOutOfGivenTotals()
    {
        var owner = await CreateUser("chgdelown");
        var controller = await CreateUser("chgdelctl");
        var shockerId = await CreateShocker(WebApplicationFactory, owner, "Deleted");
        await Insert(WebApplicationFactory,
            Log(owner, shockerId, controller, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        await RefreshControlStats(WebApplicationFactory);

        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            await db.Users.Where(x => x.Id == owner).ExecuteDeleteAsync();
        }

        using var client = await CreateTokenClient([AutomationTokenType.ControlStatsRead]);
        using var doc = await Post(client, new { userIds = new[] { controller } });

        var user = doc.RootElement.GetProperty("users").EnumerateArray().Single();
        await Assert.That(user.GetProperty("userId").GetGuid()).IsEqualTo(controller);
        await Assert.That(user.GetProperty("given").GetArrayLength()).IsEqualTo(0);
    }

    /// <summary>Moves the users' logs out of the change lookback, as if written long ago.</summary>
    private async Task Backdate(params Guid[] ownerIds)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var longAgo = DateTime.UtcNow.AddDays(-1);
        await db.ShockerControlLogs.Where(x => ownerIds.Contains(x.OwnerUserId))
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.InsertedAt, longAgo));
    }

    private static JsonElement SingleShock(JsonElement totals) => totals.EnumerateArray()
        .Single(x => x.GetProperty("type").GetString() == "Shock" && !x.GetProperty("live").GetBoolean());

    private static async Task<JsonDocument> Post(HttpClient client, object body)
    {
        var response = await client.PostAsync(Endpoint, TestHelper.JsonContent(body));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private Task<Guid> CreateUser(string prefix) => TestHelper.CreateUserInDb(WebApplicationFactory,
        TestHelper.UniqueUsername(prefix), TestHelper.UniqueEmail(prefix), Password);

    private async Task<HttpClient> CreateTokenClient(List<AutomationTokenType> types)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var secret = IAutomationTokenService.GenerateSecret();
        db.AutomationTokens.Add(new AutomationToken
        {
            Id = Guid.CreateVersion7(),
            Name = "control-stats-test",
            TokenHash = HashingUtils.HashToken(secret),
            Types = types
        });
        await db.SaveChangesAsync();

        var client = WebApplicationFactory.CreateClient();
        client.DefaultRequestHeaders.Add(AuthConstants.AutomationTokenHeaderName, secret);
        return client;
    }
}