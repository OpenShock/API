using System.Net;
using System.Text.Json;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class TokensTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Create Token ---

    [Test]
    public async Task CreateToken_Success_ReturnsTokenString()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokcreate", "tokcreate@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/tokens", TestHelper.JsonContent(new
        {
            name = "MyToken",
            permissions = new[] { "shockers.use" }
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        await Assert.That(root.GetProperty("token").GetString()).IsNotNullOrWhiteSpace();
        await Assert.That(root.GetProperty("name").GetString()).IsEqualTo("MyToken");
        await Assert.That(root.TryGetProperty("id", out _)).IsTrue();
    }

    // --- Create Token V2 (shocker control) ---

    private static object ShockerControlBody(bool paused = false) => new
    {
        paused,
        intensity = new { min = 0, max = 100, mode = "Clamp" },
        duration = new { min = 300, max = 65535, mode = "Clamp" }
    };

    [Test]
    public async Task CreateTokenV2_MissingShockerControl_Returns400()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokv2def", "tokv2def@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // shockerControl is required on v2 create.
        var response = await client.PostAsync("/2/tokens", TestHelper.JsonContent(new
        {
            name = "NoShockerControl",
            permissions = new[] { "shockers.use" }
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateTokenV2_CustomShockerControl_RoundTrips()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokv2custom", "tokv2custom@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var createResponse = await client.PostAsync("/2/tokens", TestHelper.JsonContent(new
        {
            name = "CustomToken",
            permissions = new[] { "shockers.use" },
            shockerControl = new
            {
                paused = true,
                intensity = new { min = 10, max = 50, mode = "Lerp" },
                duration = new { min = 500, max = 2000, mode = "Clamp" }
            }
        }));

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var tokenId = createDoc.RootElement.GetProperty("id").GetString();

        // Read it back via v2 GET and confirm the configuration persisted.
        var getResponse = await client.GetAsync($"/2/tokens/{tokenId}");
        var json = await getResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var sc = doc.RootElement.GetProperty("shockerControl");
        await Assert.That(sc.GetProperty("paused").GetBoolean()).IsTrue();
        await Assert.That(sc.GetProperty("intensity").GetProperty("min").GetInt32()).IsEqualTo(10);
        await Assert.That(sc.GetProperty("intensity").GetProperty("max").GetInt32()).IsEqualTo(50);
        await Assert.That(sc.GetProperty("intensity").GetProperty("mode").GetString()).IsEqualTo("Lerp");
        await Assert.That(sc.GetProperty("duration").GetProperty("min").GetInt32()).IsEqualTo(500);
        await Assert.That(sc.GetProperty("duration").GetProperty("max").GetInt32()).IsEqualTo(2000);
    }

    [Test]
    public async Task CreateTokenV2_MinGreaterThanMax_Returns400()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokv2minmax", "tokv2minmax@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/2/tokens", TestHelper.JsonContent(new
        {
            name = "BadToken",
            permissions = new[] { "shockers.use" },
            shockerControl = new
            {
                paused = false,
                intensity = new { min = 80, max = 20, mode = "Clamp" },
                duration = new { min = 300, max = 65535, mode = "Clamp" }
            }
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateTokenV2_IntensityOutOfRange_Returns400()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokv2range", "tokv2range@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/2/tokens", TestHelper.JsonContent(new
        {
            name = "OutOfRange",
            permissions = new[] { "shockers.use" },
            shockerControl = new
            {
                paused = false,
                intensity = new { min = 0, max = 200, mode = "Clamp" },
                duration = new { min = 300, max = 65535, mode = "Clamp" }
            }
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task SetTokenPaused_TogglesAndReturnsState()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokv2pause", "tokv2pause@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var createResponse = await client.PostAsync("/2/tokens", TestHelper.JsonContent(new
        {
            name = "PauseMe",
            permissions = new[] { "shockers.use" },
            shockerControl = ShockerControlBody(paused: false)
        }));
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var tokenId = createDoc.RootElement.GetProperty("id").GetString();

        // Pause it; the endpoint returns the now-set state.
        var pauseResponse = await client.PatchAsync($"/2/tokens/{tokenId}/paused", TestHelper.JsonContent(new { paused = true }));
        await Assert.That(pauseResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using (var pauseDoc = JsonDocument.Parse(await pauseResponse.Content.ReadAsStringAsync()))
        {
            await Assert.That(pauseDoc.RootElement.GetProperty("paused").GetBoolean()).IsTrue();
        }

        // Confirm it persisted.
        var getResponse = await client.GetAsync($"/2/tokens/{tokenId}");
        using var getDoc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        await Assert.That(getDoc.RootElement.GetProperty("shockerControl").GetProperty("paused").GetBoolean()).IsTrue();
    }

    [Test]
    public async Task SetTokenPaused_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokv2pause404", "tokv2pause404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PatchAsync($"/2/tokens/{Guid.CreateVersion7()}/paused", TestHelper.JsonContent(new { paused = true }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- List Tokens ---

    [Test]
    public async Task ListTokens_ReturnsCreatedTokens()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "toklist", "toklist@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Create two tokens
        await client.PostAsync("/1/tokens", TestHelper.JsonContent(new { name = "Token1", permissions = new[] { "shockers.use" } }));
        await client.PostAsync("/1/tokens", TestHelper.JsonContent(new { name = "Token2", permissions = new[] { "shockers.use" } }));

        var response = await client.GetAsync("/1/tokens");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        await Assert.That(doc.RootElement.GetArrayLength()).IsGreaterThanOrEqualTo(2);
    }

    // --- Get Token by ID ---

    [Test]
    public async Task GetTokenById_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokgetid", "tokgetid@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Create a token
        var createResponse = await client.PostAsync("/1/tokens", TestHelper.JsonContent(new
        {
            name = "GetMe",
            permissions = new[] { "shockers.use" }
        }));
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var tokenId = createDoc.RootElement.GetProperty("id").GetString();

        var response = await client.GetAsync($"/1/tokens/{tokenId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        await Assert.That(doc.RootElement.GetProperty("name").GetString()).IsEqualTo("GetMe");
    }

    [Test]
    public async Task GetTokenById_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokget404", "tokget404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync($"/1/tokens/{Guid.CreateVersion7()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Edit Token ---

    [Test]
    public async Task EditToken_ChangeName_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokedit", "tokedit@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Create
        var createResponse = await client.PostAsync("/1/tokens", TestHelper.JsonContent(new
        {
            name = "OldName",
            permissions = new[] { "shockers.use" }
        }));
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var tokenId = createDoc.RootElement.GetProperty("id").GetString();

        // Edit
        var editResponse = await client.PatchAsync($"/1/tokens/{tokenId}", TestHelper.JsonContent(new
        {
            name = "NewName",
            permissions = new[] { "shockers.use", "shockers.edit" }
        }));

        await Assert.That(editResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify name changed
        var getResponse = await client.GetAsync($"/1/tokens/{tokenId}");
        var json = await getResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        await Assert.That(doc.RootElement.GetProperty("name").GetString()).IsEqualTo("NewName");
    }

    [Test]
    public async Task EditToken_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokedit404", "tokedit404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PatchAsync($"/1/tokens/{Guid.CreateVersion7()}", TestHelper.JsonContent(new
        {
            name = "Nope",
            permissions = new[] { "shockers.use" }
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Delete Token ---

    [Test]
    public async Task DeleteToken_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokdel", "tokdel@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Create
        var createResponse = await client.PostAsync("/1/tokens", TestHelper.JsonContent(new
        {
            name = "ToDelete",
            permissions = new[] { "shockers.use" }
        }));
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var tokenId = createDoc.RootElement.GetProperty("id").GetString();

        // Delete
        var deleteResponse = await client.DeleteAsync($"/1/tokens/{tokenId}");
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify it's gone
        var getResponse = await client.GetAsync($"/1/tokens/{tokenId}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteToken_Nonexistent_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "tokdel404", "tokdel404@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.DeleteAsync($"/1/tokens/{Guid.CreateVersion7()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Token Self (API Token Auth) ---

    [Test]
    public async Task GetTokenSelf_WithApiToken_ReturnsInfo()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "tokself", "tokself@test.org", "SecurePassword123#");
        var (_, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, userId, "SelfToken");
        using var client = TestHelper.CreateApiTokenClient(WebApplicationFactory, rawToken);

        var response = await client.GetAsync("/1/tokens/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        await Assert.That(doc.RootElement.GetProperty("name").GetString()).IsEqualTo("SelfToken");
    }

    // --- API Token Auth for other endpoints ---

    [Test]
    public async Task ApiTokenAuth_CanAccessDevices()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "tokauth", "tokauth@test.org", "SecurePassword123#");
        var (_, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, userId, "AuthToken",
            [PermissionType.Shockers_Use, PermissionType.Devices_Edit]);
        using var client = TestHelper.CreateApiTokenClient(WebApplicationFactory, rawToken);

        var response = await client.GetAsync("/1/devices");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    // --- Unauthorized ---

    [Test]
    public async Task ListTokens_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1/tokens");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
