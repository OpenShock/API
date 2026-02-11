using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OpenShock.API.IntegrationTests.Helpers;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class DevicesTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- List Devices ---

    [Test]
    public async Task ListDevices_Empty_ReturnsEmptyArray()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devempty", "devempty@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync("/1/devices");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetArrayLength()).IsEqualTo(0);
    }

    // --- Create Device (V1 + V2) ---

    [Test]
    public async Task CreateDeviceV1_Returns201()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devv1create", "devv1create@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/devices", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task CreateDeviceV2_WithName_Returns201()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devv2create", "devv2create@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/2/devices", TestHelper.JsonContent(new
        {
            name = "My Custom Hub"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    // --- Get Device by ID ---

    [Test]
    public async Task GetDeviceById_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devgetone", "devgetone@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id, "TestHub1");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync($"/1/devices/{deviceId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetProperty("name").GetString()).IsEqualTo("TestHub1");
    }

    [Test]
    public async Task GetDeviceById_NonexistentId_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devget404", "devget404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync($"/1/devices/{Guid.CreateVersion7()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetDeviceById_OtherUsersDevice_Returns404()
    {
        var user1 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devowner", "devowner@test.org", "SecurePassword123#");
        var user2 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devother", "devother@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user1.Id, "OwnerHub");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user2.SessionToken);

        var response = await client.GetAsync($"/1/devices/{deviceId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Edit Device ---

    [Test]
    public async Task EditDevice_Rename_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devedit", "devedit@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id, "OldName");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PatchAsync($"/1/devices/{deviceId}", TestHelper.JsonContent(new
        {
            name = "RenamedHub"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify name changed
        var getResponse = await client.GetAsync($"/1/devices/{deviceId}");
        var json = await getResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var name = doc.RootElement.GetProperty("data").GetProperty("name").GetString();
        await Assert.That(name).IsEqualTo("RenamedHub");
    }

    [Test]
    public async Task EditDevice_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devedit404", "devedit404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PatchAsync($"/1/devices/{Guid.CreateVersion7()}", TestHelper.JsonContent(new
        {
            name = "Whatever"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Delete Device ---

    [Test]
    public async Task DeleteDevice_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devdel", "devdel@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id, "ToDelete");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.DeleteAsync($"/1/devices/{deviceId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify it no longer exists
        var getResponse = await client.GetAsync($"/1/devices/{deviceId}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteDevice_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devdel404", "devdel404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.DeleteAsync($"/1/devices/{Guid.CreateVersion7()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Regenerate Device Token ---

    [Test]
    public async Task RegenerateDeviceToken_ReturnsNewToken()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devregen", "devregen@test.org", "SecurePassword123#");
        var (deviceId, originalToken) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id, "RegenHub");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PutAsync($"/1/devices/{deviceId}", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var newToken = await response.Content.ReadAsStringAsync();
        await Assert.That(newToken).IsNotNullOrWhiteSpace();
        await Assert.That(newToken).IsNotEqualTo(originalToken);
    }

    // --- Get Device Shockers ---

    [Test]
    public async Task GetDeviceShockers_EmptyDevice_ReturnsEmptyList()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devshock0", "devshock0@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id, "EmptyHub");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync($"/1/devices/{deviceId}/shockers");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task GetDeviceShockers_WrongDevice_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "devshock404", "devshock404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync($"/1/devices/{Guid.CreateVersion7()}/shockers");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Unauthorized ---

    [Test]
    public async Task ListDevices_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1/devices");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
