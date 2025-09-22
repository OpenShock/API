using System.Net;
using System.Text.Json;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class MetadataTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }
    
    [Test]
    public async Task GetMetadata_ShouldMatchBackendInfoResponseContract()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        await Assert.That(mediaType).IsEqualTo("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        
        // Validate Message
        var message = root.GetProperty("message").GetString();
        await Assert.That(message).IsEqualTo("OpenShock");
        
        // Fetch data
        var data = root.GetProperty("data");

        // Validate Version
        var version = data.GetProperty("version").GetString();
        await Assert.That(version).IsNotNullOrWhitespace();

        // Validate Commit
        var commit = data.GetProperty("commit").GetString();
        await Assert.That(commit).Matches("[a-zA-Z0-9]{4,64}");

        // Validate CurrentTime
        var currentTime = data.GetProperty("currentTime").GetDateTimeOffset();
        await Assert.That(currentTime).IsBetween(
            DateTimeOffset.UtcNow.AddSeconds(-5),
            DateTimeOffset.UtcNow.AddSeconds(5)
        );

        // Validate FrontendUrl
        var frontendUrlStr = data.GetProperty("frontendUrl").GetString();
        await Assert.That(Uri.TryCreate(frontendUrlStr, UriKind.Absolute, out _))
            .IsTrue();

        // Validate ShortLinkUrl
        var shortLinkUrlStr = data.GetProperty("shortLinkUrl").GetString();
        await Assert.That(Uri.TryCreate(shortLinkUrlStr, UriKind.Absolute, out _))
            .IsTrue();

        // Validate TurnstileSiteKey (nullable, can be null or string)
        var turnstileSiteKeyProp = data.GetProperty("turnstileSiteKey");
        if (turnstileSiteKeyProp.ValueKind is not JsonValueKind.Null)
        {
            var turnstileSiteKey = turnstileSiteKeyProp.GetString();
            await Assert.That(turnstileSiteKey).IsNotNullOrWhitespace();
        }

        // Validate OAuthProviders (string[])
        var oauthProviders = data.GetProperty("oAuthProviders");
        await Assert.That(oauthProviders.ValueKind).IsEqualTo(JsonValueKind.Array);
        foreach (var provider in oauthProviders.EnumerateArray())
        {
            var p = provider.GetString();
            await Assert.That(p).IsNotNullOrWhitespace();
        }

        // Validate IsUserAuthenticated (bool)
        var isUserAuthenticated = data.GetProperty("isUserAuthenticated").GetBoolean();
        await Assert.That(isUserAuthenticated).IsIn(true, false);
    }
}