using System.Net;
using System.Text.Json;
using OpenShock.API.IntegrationTests.Helpers;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class SessionsTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Get Self Session ---

    [Test]
    public async Task GetSelfSession_ReturnsSessionInfo()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sessself", "sessself@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync("/1/sessions/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("id", out _)).IsTrue();
    }

    // --- Delete Session ---

    [Test]
    public async Task DeleteSession_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sessdel404", "sessdel404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.DeleteAsync($"/1/sessions/{Guid.CreateVersion7()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteSession_OwnSession_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sessdel", "sessdel@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Get self session to get session ID
        var selfResponse = await client.GetAsync("/1/sessions/self");
        await Assert.That(selfResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var selfJson = await selfResponse.Content.ReadAsStringAsync();
        using var selfDoc = JsonDocument.Parse(selfJson);
        var sessionId = selfDoc.RootElement.GetProperty("id").GetString();

        // Delete it
        var response = await client.DeleteAsync($"/1/sessions/{sessionId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    // --- Unauthenticated ---

    [Test]
    public async Task GetSelfSession_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1/sessions/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteSession_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.DeleteAsync($"/1/sessions/{Guid.CreateVersion7()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
