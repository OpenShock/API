using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.AutomationTokens;
using OpenShock.Common.Utils;

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
        var shockerId = await CreateShocker(owner, "Changes");
        var at = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        await Upsert(
            Delta(shockerId, controller, at, count: 2, intensity: 40),
            Delta(shockerId, notRequested, at, count: 7));

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
        var shockerId = await CreateShocker(owner, "Since");
        var quietShockerId = await CreateShocker(quiet, "Quiet");

        // Old activity only, outside of any lookback
        var old = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await Upsert(Delta(shockerId, owner, old, count: 3), Delta(quietShockerId, quiet, old, count: 1));

        using var client = await CreateTokenClient([AutomationTokenType.ControlStatsRead]);
        var userIds = new[] { owner, quiet };

        DateTime cursor;
        using (var initial = await Post(client, new { userIds }))
            cursor = initial.RootElement.GetProperty("cursor").GetDateTime();

        using (var unchanged = await Post(client, new { since = cursor, userIds }))
            await Assert.That(unchanged.RootElement.GetProperty("users").GetArrayLength()).IsEqualTo(0);

        await Upsert(Delta(shockerId, owner, DateTime.UtcNow, count: 1));

        using var changed = await Post(client, new { since = cursor, userIds });
        var user = changed.RootElement.GetProperty("users").EnumerateArray().Single();
        await Assert.That(user.GetProperty("userId").GetGuid()).IsEqualTo(owner);
        await Assert.That(SingleShock(user.GetProperty("received")).GetProperty("count").GetInt64()).IsEqualTo(4);
        await Assert.That(SingleShock(user.GetProperty("given")).GetProperty("count").GetInt64()).IsEqualTo(4);
    }

    private static ControlStatDelta Delta(Guid shockerId, Guid? controllerId, DateTime at, long count, byte intensity = 50) =>
        new(shockerId, controllerId, at, ControlType.Shock, Live: false, count, DurationMs: count * 1000,
            IntensityMs: count * 1000 * intensity, intensity);

    private static JsonElement SingleShock(JsonElement totals) => totals.EnumerateArray()
        .Single(x => x.GetProperty("type").GetString() == "Shock" && !x.GetProperty("live").GetBoolean());

    private async Task Upsert(params ControlStatDelta[] deltas)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OpenShockContext>().UpsertControlStatsAsync(deltas);
    }

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
}
