using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class ShockersTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Register Shocker ---

    [Test]
    public async Task RegisterShocker_Success_Returns201()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkreg", "shkreg@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id, "ShkHub");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/shockers", TestHelper.JsonContent(new
        {
            name = "TestShocker",
            rfId = 1234,
            device = deviceId,
            model = "CaiXianlin"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        // Should return a GUID
        await Assert.That(Guid.TryParse(data.GetString(), out _)).IsTrue();
    }

    [Test]
    public async Task RegisterShocker_NonexistentDevice_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkregbad", "shkregbad@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/shockers", TestHelper.JsonContent(new
        {
            name = "Ghost",
            rfId = 9999,
            device = Guid.CreateVersion7(),
            model = "CaiXianlin"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Get Shocker by ID ---

    [Test]
    public async Task GetShockerById_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkget", "shkget@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id);
        var shockerId = await CreateShockerInDb(user.Id, deviceId, "MyShocker", 100);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync($"/1/shockers/{shockerId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetProperty("name").GetString()).IsEqualTo("MyShocker");
    }

    [Test]
    public async Task GetShockerById_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkget404", "shkget404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync($"/1/shockers/{Guid.CreateVersion7()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Edit Shocker ---

    [Test]
    public async Task EditShocker_Rename_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkedit", "shkedit@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id);
        var shockerId = await CreateShockerInDb(user.Id, deviceId, "OldShockerName", 200);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PatchAsync($"/1/shockers/{shockerId}", TestHelper.JsonContent(new
        {
            name = "RenamedShocker",
            rfId = 200,
            device = deviceId,
            model = "CaiXianlin"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify the name changed
        var getResponse = await client.GetAsync($"/1/shockers/{shockerId}");
        var json = await getResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var name = doc.RootElement.GetProperty("data").GetProperty("name").GetString();
        await Assert.That(name).IsEqualTo("RenamedShocker");
    }

    [Test]
    public async Task EditShocker_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkedit404", "shkedit404@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PatchAsync($"/1/shockers/{Guid.CreateVersion7()}", TestHelper.JsonContent(new
        {
            name = "Whatever",
            rfId = 100,
            device = deviceId,
            model = "CaiXianlin"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Pause / Unpause ---

    [Test]
    public async Task PauseShocker_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkpause", "shkpause@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id);
        var shockerId = await CreateShockerInDb(user.Id, deviceId, "PauseShocker", 300);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Pause
        var pauseResponse = await client.PostAsync($"/1/shockers/{shockerId}/pause", TestHelper.JsonContent(new
        {
            pause = true
        }));
        await Assert.That(pauseResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify paused
        var getResponse = await client.GetAsync($"/1/shockers/{shockerId}");
        var json = await getResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var isPaused = doc.RootElement.GetProperty("data").GetProperty("isPaused").GetBoolean();
        await Assert.That(isPaused).IsTrue();

        // Unpause
        var unpauseResponse = await client.PostAsync($"/1/shockers/{shockerId}/pause", TestHelper.JsonContent(new
        {
            pause = false
        }));
        await Assert.That(unpauseResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task PauseShocker_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkpause404", "shkpause404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync($"/1/shockers/{Guid.CreateVersion7()}/pause", TestHelper.JsonContent(new
        {
            pause = true
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Delete Shocker ---

    [Test]
    public async Task DeleteShocker_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkdel", "shkdel@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id);
        var shockerId = await CreateShockerInDb(user.Id, deviceId, "ToDelete", 400);
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.DeleteAsync($"/1/shockers/{shockerId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify it no longer exists
        var getResponse = await client.GetAsync($"/1/shockers/{shockerId}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteShocker_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "shkdel404", "shkdel404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.DeleteAsync($"/1/shockers/{Guid.CreateVersion7()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Unauthorized ---

    [Test]
    public async Task RegisterShocker_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/shockers", TestHelper.JsonContent(new
        {
            name = "Test",
            rfId = 1,
            device = Guid.CreateVersion7(),
            model = "CaiXianlin"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- Helper ---

    private async Task<Guid> CreateShockerInDb(Guid userId, Guid deviceId, string name, ushort rfId)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var shockerId = Guid.CreateVersion7();
        db.Shockers.Add(new Shocker
        {
            Id = shockerId,
            Name = name,
            RfId = rfId,
            DeviceId = deviceId,
            Model = ShockerModelType.CaiXianlin
        });
        await db.SaveChangesAsync();
        return shockerId;
    }
}
