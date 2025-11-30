using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM.Contracts;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class LcgAssignmentTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }
    
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HubId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string HubToken = "test";

    [Before(Test)]
    public async Task Setup()
    {
        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var db = context.ServiceProvider.GetRequiredService<OpenShockContext>();

        var user = new User
        {
            Id = UserId,
            Name = "TestUser",
            Email = "test@test.org",
            PasswordHash = HashingUtils.HashPassword("password"),
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = DateTime.UtcNow
        };
        
        db.Users.Add(user);

        var hub = new Device
        {
            Id = HubId,
            Name = "TestHub",
            OwnerId = UserId,
            Token = HubToken,
            CreatedAt = DateTime.UtcNow
        };
        
        db.Devices.Add(hub);
        
        await db.SaveChangesAsync();
    }

    [After(Test)]
    public async Task Teardown()
    {
        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var db = context.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.Devices.Where(x => x.Id == HubId).ExecuteDeleteAsync();
        await db.Users.Where(x => x.Id == UserId).ExecuteDeleteAsync();
        
        var redisConnectionProvider = context.ServiceProvider.GetRequiredService<IRedisConnectionProvider>();
        var webHostEnvironment = context.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var lcgNodesCollection = redisConnectionProvider.RedisCollection<LcgNode>(false);

        var allLcg = await lcgNodesCollection.ToArrayAsync();
        await lcgNodesCollection.DeleteAsync(allLcg);
    }

    [Test]
    [NotInParallel]
    [Arguments("US", "us1.example.com", new[] { "US|us1.example.com", "DE|de1.example.com", "AS|as1.example.com" })]
    [Arguments("DE", "de1.example.com", new[] { "US|us1.example.com", "DE|de1.example.com", "AS|as1.example.com" })]
    [Arguments("CA", "us1.example.com", new[] { "US|us1.example.com", "DE|de1.example.com", "AS|as1.example.com" })]
    [Arguments("CA", "us1.example.com", new[] { "US|us1.example.com", "DE|de1.example.com", "AS|as1.example.com" })]
    [Arguments("AT", "de1.example.com", new[] { "US|us1.example.com", "DE|de1.example.com", "AS|as1.example.com" })]
    [Arguments("FR", "de1.example.com", new[] { "US|us1.example.com", "DE|de1.example.com", "AS|as1.example.com" })]
    public async Task GetLcgAssignment(string requesterCountry, string expectedHost, string[] availableGateways)
    {
        using var client = WebApplicationFactory.CreateClient();

        await using var context = WebApplicationFactory.Services.CreateAsyncScope();
        var redisConnectionProvider = context.ServiceProvider.GetRequiredService<IRedisConnectionProvider>();
        var webHostEnvironment = context.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var lcgNodesCollection = redisConnectionProvider.RedisCollection<LcgNode>(false);

        var testGateways = availableGateways.Select(x =>
        {
            var split = x.Split('|');
            if (split.Length != 2)
                throw new ArgumentException("Invalid gateway format");

            return new LcgNode
            {
                Country = split[0],
                Fqdn = split[1],
                Load = 0,
                Environment = webHostEnvironment.EnvironmentName
            };
        });
        
        await lcgNodesCollection.InsertAsync(testGateways);

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/2/device/assignLCG?version=1");
        httpRequest.Headers.Add("Device-Token", HubToken);
        httpRequest.Headers.Add("CF-IPCountry", requesterCountry);
        var response = await client.SendAsync(httpRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(mediaType).IsEqualTo("application/json");

        var data = await response.Content.ReadFromJsonAsync<LcgNodeResponseV2>();
        await Assert.That(data).IsNotNull();
        await Assert.That(data.Host).IsEqualTo(expectedHost);
    }
    
    
}