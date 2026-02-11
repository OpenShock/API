using System.Net;
using System.Text.Json;
using OpenShock.API.IntegrationTests.Helpers;

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
        var (tokenId, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, userId, "SelfToken");
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
            [Common.Models.PermissionType.Shockers_Use, Common.Models.PermissionType.Devices_Edit]);
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
