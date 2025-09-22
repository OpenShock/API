using System.Net;
using System.Net.Http.Json;

namespace OpenShock.API.IntegrationTests.Tests;

file sealed class Parent
{
    public required string Message { get; init; }
    public required BackendInfoResponse Data { get; init; }
}
file sealed class BackendInfoResponse
{
    public required string Version { get; init; }
    public required string Commit { get; init; }
    public required DateTimeOffset CurrentTime { get; init; }
    public required Uri FrontendUrl { get; init; }
    public required Uri ShortLinkUrl { get; init; }
    public required string? TurnstileSiteKey { get; init; }
    public required string[] OAuthProviders { get; init; }
    public required bool IsUserAuthenticated { get; init; }
}

public class MetadataTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }
    
    [Test]
    public async Task GetMatadata_ShouldReturnOk()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var parent = await response.Content.ReadFromJsonAsync<Parent>();
        await Assert.That(parent).IsNotNull();
        await Assert.That(parent!.Message).IsEqualTo("OpenShock");

        var content = parent.Data;
        await Assert.That(content).IsNotNull();
        
        await Assert.That(content!.Version).IsNotNullOrWhitespace();
        await Assert.That(content!.Commit).IsNotNullOrWhitespace();
        await Assert.That(content!.CurrentTime).IsBetween(DateTimeOffset.UtcNow.AddSeconds(-5), DateTimeOffset.UtcNow.AddSeconds(5)); // Idk if this is ok way to do it
        await Assert.That(content!.TurnstileSiteKey).IsEqualTo("turnstile-site-key");
    }
}