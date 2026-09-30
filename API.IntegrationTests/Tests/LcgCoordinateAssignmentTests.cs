using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.Models.Response;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Services.Geo;
using OpenShock.Common.Utils;
using OpenShock.Internal.Common.Utils;
using Redis.OM.Contracts;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class LcgCoordinateAssignmentTests
{
    [ClassDataSource<GeoLocatedWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required GeoLocatedWebApplicationFactory WebApplicationFactory { get; init; }

    // Shares the key with LcgAssignmentTests: both suites wipe the same Redis node collection.
    private const string ParalellGateway = "gateway_assignment";

    private static readonly GeoPoint Seattle = new(47.61, -122.33);
    private static readonly GeoPoint NewYork = new(40.71, -74.01);
    private static readonly GeoPoint Oregon = new(45.60, -121.18);
    private static readonly GeoPoint Virginia = new(39.04, -77.49);
    private static readonly GeoPoint Frankfurt = new(50.11, 8.68);
    private static readonly GeoPoint Munich = new(48.14, 11.58);
    private static readonly GeoPoint Hamburg = new(53.55, 9.99);
    private static readonly GeoPoint Vienna = new(48.21, 16.37);

    private Guid _userId;
    private Guid _hubId;
    private string _hubToken = string.Empty;

    [Before(Test)]
    public async Task Setup()
    {
        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var db = context.ServiceProvider.GetRequiredService<OpenShockContext>();

        _userId = Guid.CreateVersion7();
        _hubId = Guid.CreateVersion7();
        _hubToken = CryptoUtils.RandomString(256);

        db.Users.Add(new User
        {
            Id = _userId,
            Name = _userId.ToString("N"),
            Email = $"{_userId}@test.org",
            PasswordHash = HashingUtils.HashPassword("password")
        });
        db.Devices.Add(new Device
        {
            Id = _hubId,
            Name = "TestHub",
            OwnerId = _userId,
            Token = _hubToken,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [After(Test)]
    public async Task Teardown()
    {
        WebApplicationFactory.ClientLocation = null;

        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var db = context.ServiceProvider.GetRequiredService<OpenShockContext>();
        var lcgNodesCollection = context.ServiceProvider.GetRequiredService<IRedisConnectionProvider>().RedisCollection<LcgNode>(false);

        await db.Devices.Where(x => x.Id == _hubId).ExecuteDeleteAsync();
        await db.Users.Where(x => x.Id == _userId).ExecuteDeleteAsync();

        var allLcg = await lcgNodesCollection.ToListAsync();
        await lcgNodesCollection.DeleteAsync(allLcg);
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task PicksClosestGatewayWithinSameCountry()
    {
        // Both gateways are "US", so the country table alone ties them; coordinates break the tie.
        await AddGateway("us-west.example.com", "US", Oregon);
        await AddGateway("us-east.example.com", "US", Virginia);

        WebApplicationFactory.ClientLocation = Seattle;
        await Assert.That(await AssignHost("US")).IsEqualTo("us-west.example.com");

        WebApplicationFactory.ClientLocation = NewYork;
        await Assert.That(await AssignHost("US")).IsEqualTo("us-east.example.com");
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task UsesCoordinatesWithoutCountryHeader()
    {
        await AddGateway("us-west.example.com", "US", Oregon);
        await AddGateway("de1.example.com", "DE", Frankfurt);

        WebApplicationFactory.ClientLocation = Seattle;
        await Assert.That(await AssignHost(null)).IsEqualTo("us-west.example.com");
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task FallsBackToCountryWhenClientHasNoLocation()
    {
        await AddGateway("us-west.example.com", "US", Oregon);
        await AddGateway("de1.example.com", "DE", Frankfurt);

        WebApplicationFactory.ClientLocation = null;
        await Assert.That(await AssignHost("DE")).IsEqualTo("de1.example.com");
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task FallsBackToCountryForGatewayWithoutLocation()
    {
        // A gateway that has not configured coordinates must still be assignable by country.
        await AddGateway("us-west.example.com", "US", Oregon);
        await AddGateway("de1.example.com", "DE", null);

        WebApplicationFactory.ClientLocation = Frankfurt;
        await Assert.That(await AssignHost("DE")).IsEqualTo("de1.example.com");
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task CoordinatesOverrideLoadWithinRegion()
    {
        // Baseline for MixedCoordinatesLeaveRegionUntouched: with full coordinates, distance wins over load.
        await AddGateway("us-west.example.com", "US", Oregon, load: 5);
        await AddGateway("us-east.example.com", "US", Virginia, load: 0);

        WebApplicationFactory.ClientLocation = Seattle;
        await Assert.That(await AssignHost("US")).IsEqualTo("us-west.example.com");
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task MixedCoordinatesLeaveRegionUntouched()
    {
        // us-east has no coordinates, so the US region can't be ranked fairly and must fall back to
        // pure load balancing, rather than us-east winning on a 0 km same-country score.
        await AddGateway("us-west.example.com", "US", Oregon, load: 0);
        await AddGateway("us-east.example.com", "US", null, load: 5);

        WebApplicationFactory.ClientLocation = NewYork;
        await Assert.That(await AssignHost("US")).IsEqualTo("us-west.example.com");
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task CoordinatesDoNotCrossCountryPick()
    {
        // Munich is nearer Vienna than Hamburg, but coordinates only refine the country pick.
        await AddGateway("de1.example.com", "DE", Hamburg);
        await AddGateway("at1.example.com", "AT", Vienna);

        WebApplicationFactory.ClientLocation = Munich;
        await Assert.That(await AssignHost("DE")).IsEqualTo("de1.example.com");
    }

    private async Task AddGateway(string host, string country, GeoPoint? location, byte load = 0)
    {
        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var environment = context.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var lcgNodesCollection = context.ServiceProvider.GetRequiredService<IRedisConnectionProvider>().RedisCollection<LcgNode>(false);

        await lcgNodesCollection.InsertAsync(new LcgNode
        {
            Id = host,
            Host = host,
            Port = 443,
            Country = country,
            Latitude = location?.Latitude,
            Longitude = location?.Longitude,
            Load = load,
            Environment = environment.EnvironmentName
        });
    }

    private async Task<string> AssignHost(string? requesterCountry)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/2/device/assignLCG?version=2");
        httpRequest.Headers.Add("Device-Token", _hubToken);
        if (!string.IsNullOrEmpty(requesterCountry)) httpRequest.Headers.Add("CF-IPCountry", requesterCountry);

        using var client = WebApplicationFactory.CreateClient();
        using var response = await client.SendAsync(httpRequest);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<LcgNodeResponseV2>();
        await Assert.That(data).IsNotNull();
        return data!.Host;
    }
}
