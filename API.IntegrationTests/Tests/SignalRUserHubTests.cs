using System.Net;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Constants;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class SignalRUserHubTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Negotiate endpoint tests (HTTP-based, works with TestServer) ---

    [Test]
    public async Task Negotiate_WithValidSession_ReturnsOk()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "hubneg", "hubneg@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Negotiate_WithApiToken_ReturnsOk()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "hubnegapi", "hubnegapi@test.org", "SecurePassword123#");
        var (_, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, user.Id, "hub-negotiate-token");
        using var client = TestHelper.CreateApiTokenClient(WebApplicationFactory, rawToken);

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Negotiate_WithoutAuth_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Negotiate_WithInvalidSession_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthConstants.UserSessionCookieName}=invalid-session-token");

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Negotiate_WithHubToken_Returns401()
    {
        // Hub/device tokens should NOT work on user hub
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "hubneghub", "hubneghub@test.org", "SecurePassword123#");
        var (_, hubToken) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id, "NegTestDevice");
        using var client = TestHelper.CreateHubTokenClient(WebApplicationFactory, hubToken);

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- Negotiate response content ---

    [Test]
    public async Task Negotiate_ReturnsTransportInfo()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "hubneginfo", "hubneginfo@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/hubs/user/negotiate?negotiateVersion=1", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        // Negotiate response should contain connectionId and available transports
        await Assert.That(doc.RootElement.TryGetProperty("connectionId", out _)).IsTrue();
        await Assert.That(doc.RootElement.TryGetProperty("availableTransports", out _)).IsTrue();
    }

    // --- Public share hub ---

    [Test]
    public async Task PublicShareHub_Negotiate_WithoutAuth_ReturnsOk()
    {
        // PublicShareHub doesn't require auth at the negotiate level
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync($"/1/hubs/share/link/{Guid.NewGuid()}/negotiate?negotiateVersion=1", null);
        // Should return OK (negotiate doesn't check auth or share existence)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
