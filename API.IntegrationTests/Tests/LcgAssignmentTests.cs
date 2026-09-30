using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.Models.Response;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM.Contracts;

using OpenShock.Internal.Common.Utils;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class LcgAssignmentTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    private const string ParalellGateway = "gateway_assignment";
    private Guid _userId;
    private Guid _hubId;
    private string _hubToken = string.Empty;

    [Before(Test)]
    public async Task Setup()
    {
        // Dependency Resolution
        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var db = context.ServiceProvider.GetRequiredService<OpenShockContext>();

        // Set up variables
        _userId = Guid.CreateVersion7();
        _hubId = Guid.CreateVersion7();
        _hubToken = CryptoUtils.RandomString(256);

        // Create mock data
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
        // Dependency Resolution
        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var db = context.ServiceProvider.GetRequiredService<OpenShockContext>();
        var redisConnectionProvider = context.ServiceProvider.GetRequiredService<IRedisConnectionProvider>();
        var lcgNodesCollection = redisConnectionProvider.RedisCollection<LcgNode>(false);

        // Data cleanup
        await db.Devices.Where(x => x.Id == _hubId).ExecuteDeleteAsync();
        await db.Users.Where(x => x.Id == _userId).ExecuteDeleteAsync();

        var allLcg = await lcgNodesCollection.ToListAsync();
        await lcgNodesCollection.DeleteAsync(allLcg);
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    [Arguments("US", "us1.example.com")]
    [Arguments("DE", "de1.example.com")]
    [Arguments("CA", "us1.example.com")]
    [Arguments("CA", "us1.example.com")]
    [Arguments("AT", "de1.example.com")]
    [Arguments("FR", "de1.example.com")]
    public async Task CheckBasicAssignments(string requesterCountry, string expectedHost)
    {
        await AddGateways(["US|us1.example.com", "DE|de1.example.com", "AS|as1.example.com"]);
        var response = await SendAssignRequest(requesterCountry);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(mediaType).IsEqualTo(MediaTypeNames.Application.Json);

        var data = await response.Content.ReadFromJsonAsync<LcgNodeResponseV2>();
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Host).IsEqualTo(expectedHost);
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    [Arguments("US")]
    [Arguments("DE")]
    [Arguments("XX")]
    [Arguments(null)]
    public async Task CheckAnyGateway(string? requesterCountry)
    {
        await AddGateways(["US|us1.example.com", "DE|de1.example.com", "AS|as1.example.com"]);
        var response = await SendAssignRequest(requesterCountry);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(mediaType).IsEqualTo(MediaTypeNames.Application.Json);

        var data = await response.Content.ReadFromJsonAsync<LcgNodeResponseV2>();
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Host).IsNotNullOrWhiteSpace();
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    [Arguments("US")]
    [Arguments("XX")]
    [Arguments(null)]
    public async Task CheckUnavailable(string? requesterCountry)
    {
        // We dont add any gateways here
        var response = await SendAssignRequest(requesterCountry);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task CheckEnvironmentFilter()
    {
        await AddGateways([
            "US|us-dev.example.com",
            "DE|de-dev.example.com",
            "AS|as-dev.example.com"
        ], "SomethingThatDoesntExist!");
        
        // This sends a request with an actual environment like development or production
        var response = await SendAssignRequest("XX");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
    }

    private async Task AddGateways(string[] availableGateways, string? environmentOverride = null)
    {
        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var redisConnectionProvider = context.ServiceProvider.GetRequiredService<IRedisConnectionProvider>();
        var webHostEnvironment = context.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var lcgNodesCollection = redisConnectionProvider.RedisCollection<LcgNode>(false);
        var testGateways = availableGateways.Select(x =>
        {
            var split = x.Split('|');
            if (split.Length != 2)
                throw new ArgumentException("Invalid gateway format");

            var host = split[1];
            return new LcgNode
            {
                Id = host,
                Host = host,
                Port = 443,
                Country = split[0],
                Load = 0,
                Environment = environmentOverride ?? webHostEnvironment.EnvironmentName
            };
        });

        await lcgNodesCollection.InsertAsync(testGateways);
    }

    private async Task AddGatewayNode(string host, ushort port, string pathPrefix, string country)
    {
        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var redisConnectionProvider = context.ServiceProvider.GetRequiredService<IRedisConnectionProvider>();
        var webHostEnvironment = context.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var lcgNodesCollection = redisConnectionProvider.RedisCollection<LcgNode>(false);

        var id = host;
        if (port != 443) id += ":" + port;
        id += pathPrefix;

        await lcgNodesCollection.InsertAsync(new LcgNode
        {
            Id = id,
            Host = host,
            Port = port,
            PathPrefix = pathPrefix,
            Country = country,
            Load = 0,
            Environment = webHostEnvironment.EnvironmentName
        });
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task CheckPathAndPortPropagation()
    {
        // A gateway on a custom port and path prefix must be advertised verbatim, with the
        // firmware WS route appended to the prefix.
        await AddGatewayNode("de1.example.com", 8080, "/gateway", "DE");

        var response = await SendAssignRequest("DE", schemaVersion: 2);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<LcgNodeResponseV2>();
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Host).IsEqualTo("de1.example.com");
        await Assert.That(data.Port).IsEqualTo((ushort)8080);
        await Assert.That(data.Path).IsEqualTo("/gateway/2/ws/hub");
    }

    [Test]
    [NotInParallel(ParalellGateway)]
    public async Task CheckDefaultGatewayIsBackwardCompatible()
    {
        // Default port/root path -> bare host, port 443, unprefixed route (unchanged wire shape).
        await AddGateways(["DE|de1.example.com"]);

        var response = await SendAssignRequest("DE", schemaVersion: 2);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<LcgNodeResponseV2>();
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Host).IsEqualTo("de1.example.com");
        await Assert.That(data.Port).IsEqualTo((ushort)443);
        await Assert.That(data.Path).IsEqualTo("/2/ws/hub");
    }

    private async Task<HttpResponseMessage> SendAssignRequest(string? requesterCountry, uint schemaVersion = 1)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/2/device/assignLCG?version={schemaVersion}");
        httpRequest.Headers.Add("Device-Token", _hubToken);
        if (!string.IsNullOrEmpty(requesterCountry)) httpRequest.Headers.Add("CF-IPCountry", requesterCountry);

        using var client = WebApplicationFactory.CreateClient();
        return await client.SendAsync(httpRequest);
    }
}