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

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class LcgAssignmentTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }
    
    private Guid _userId;
    private Guid _hubId;
    private string _hubToken = string.Empty;

    [Before(Test)]
    public async Task Setup()
    {
        // Dependency Resolution
        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var db = context.ServiceProvider.GetRequiredService<OpenShockContext>();
        var redisConnectionProvider = context.ServiceProvider.GetRequiredService<IRedisConnectionProvider>();
        var webHostEnvironment = context.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var lcgNodesCollection = redisConnectionProvider.RedisCollection<LcgNode>(saveState: true);

        // Set up variables
        _userId = Guid.CreateVersion7();
        _hubId = Guid.CreateVersion7();
        _hubToken = CryptoUtils.RandomAlphaNumericString(256);
        
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
        
        (string country, string fqdn)[] availableGateways = [
            ("US","us1.example.com"),
            ("DE", "de1.example.com"),
            ("AS", "as1.example.com")
        ];
        
        await lcgNodesCollection.InsertAsync(availableGateways.Select(x => new LcgNode
        {
            Country = x.country,
            Fqdn = x.fqdn,
            Load = 0,
            Environment = webHostEnvironment.EnvironmentName
        }));
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
        
        var allLcg = await lcgNodesCollection.ToArrayAsync();
        await lcgNodesCollection.DeleteAsync(allLcg);
    }

    [Test]
    [Arguments("US", "us1.example.com")]
    [Arguments("DE", "de1.example.com")]
    [Arguments("CA", "us1.example.com")]
    [Arguments("CA", "us1.example.com")]
    [Arguments("AT", "de1.example.com")]
    [Arguments("FR", "de1.example.com")]
    public async Task GetLcgAssignment(string requesterCountry, string expectedHost)
    {
        using var client = WebApplicationFactory.CreateClient();

        await using var context = WebApplicationFactory.Services.CreateAsyncScope();

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/2/device/assignLCG?version=1");
        httpRequest.Headers.Add("Device-Token", _hubToken);
        httpRequest.Headers.Add("CF-IPCountry", requesterCountry);
        var response = await client.SendAsync(httpRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(mediaType).IsEqualTo(MediaTypeNames.Application.Json);

        var data = await response.Content.ReadFromJsonAsync<LcgNodeResponseV2>();
        await Assert.That(data).IsNotNull();
        await Assert.That(data.Host).IsEqualTo(expectedHost);
    }
}