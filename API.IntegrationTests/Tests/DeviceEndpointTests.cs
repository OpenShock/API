using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Tests for the hub/device-authenticated endpoints (/device/*).
/// These endpoints use Device-Token (HubToken) auth.
/// </summary>
public sealed class DeviceEndpointTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Get Device Self ---

    [Test]
    public async Task GetDeviceSelf_ReturnsDeviceInfo()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "hubself", "hubself@test.org", "SecurePassword123#");
        var (deviceId, hubToken) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, userId, "MyHub");
        using var client = TestHelper.CreateHubTokenClient(WebApplicationFactory, hubToken);

        var response = await client.GetAsync("/1/device/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetProperty("name").GetString()).IsEqualTo("MyHub");
        await Assert.That(data.TryGetProperty("shockers", out _)).IsTrue();
    }

    [Test]
    public async Task GetDeviceSelf_WithShockers_ReturnsShokerList()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "hubshockers", "hubshockers@test.org", "SecurePassword123#");
        var (deviceId, hubToken) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, userId, "HubWithShockers");

        // Add a shocker to this device
        await using (var scope = WebApplicationFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
            db.Shockers.Add(new Shocker
            {
                Id = Guid.CreateVersion7(),
                Name = "HubShocker",
                RfId = 500,
                DeviceId = deviceId,
                Model = ShockerModelType.CaiXianlin
            });
            await db.SaveChangesAsync();
        }

        using var client = TestHelper.CreateHubTokenClient(WebApplicationFactory, hubToken);

        var response = await client.GetAsync("/1/device/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var shockers = doc.RootElement.GetProperty("data").GetProperty("shockers");
        await Assert.That(shockers.GetArrayLength()).IsGreaterThanOrEqualTo(1);
    }

    // --- Invalid Hub Token ---

    [Test]
    public async Task GetDeviceSelf_InvalidToken_Returns401()
    {
        using var client = TestHelper.CreateHubTokenClient(WebApplicationFactory, "completely-invalid-token");

        var response = await client.GetAsync("/1/device/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- Hub token on session-only endpoint should fail ---

    [Test]
    public async Task HubToken_OnSessionOnlyEndpoint_Returns401()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "hubwrong", "hubwrong@test.org", "SecurePassword123#");
        var (_, hubToken) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, userId, "WrongHub");
        using var client = TestHelper.CreateHubTokenClient(WebApplicationFactory, hubToken);

        // Tokens endpoint requires UserSessionCookie, not HubToken
        var response = await client.GetAsync("/1/tokens");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- No auth ---

    [Test]
    public async Task GetDeviceSelf_NoAuth_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1/device/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
