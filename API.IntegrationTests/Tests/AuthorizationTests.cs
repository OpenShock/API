using System.Net;
using OpenShock.API.IntegrationTests.Helpers;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Cross-cutting authorization tests verifying that auth is enforced correctly
/// across different endpoints and auth schemes, and that cross-user isolation works.
/// </summary>
public sealed class AuthorizationTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Unauthenticated requests to protected endpoints ---

    [Test]
    [Arguments("/1/devices")]
    [Arguments("/1/shockers/shared")]
    [Arguments("/1/tokens")]
    [Arguments("/1/sessions/self")]
    [Arguments("/1/users/self")]
    public async Task ProtectedEndpoint_NoAuth_Returns401(string url)
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync(url);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ProtectedPostEndpoint_NoAuth_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/devices", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- API Token on session-only endpoints ---

    [Test]
    public async Task ApiToken_OnSessionOnlyEndpoint_Returns401()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "apitoksess", "apitoksess@test.org", "SecurePassword123#");
        var (_, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, userId, "SessionOnlyTest");
        using var client = TestHelper.CreateApiTokenClient(WebApplicationFactory, rawToken);

        // Sessions endpoint requires UserSessionCookie only
        var response = await client.GetAsync("/1/sessions/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- Session cookie on hub-only endpoint ---

    [Test]
    public async Task SessionCookie_OnHubOnlyEndpoint_Returns401()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sesshub", "sesshub@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Device self endpoint requires HubToken
        var response = await client.GetAsync("/1/device/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- Cross-user isolation: devices ---

    [Test]
    public async Task CrossUser_CannotSeeOtherUsersDevices()
    {
        var user1 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "isoown1", "isoown1@test.org", "SecurePassword123#");
        var user2 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "isoown2", "isoown2@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user1.Id, "PrivateHub");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user2.SessionToken);

        var response = await client.GetAsync($"/1/devices/{deviceId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CrossUser_CannotEditOtherUsersDevices()
    {
        var user1 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "isoedit1", "isoedit1@test.org", "SecurePassword123#");
        var user2 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "isoedit2", "isoedit2@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user1.Id, "SecureHub");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user2.SessionToken);

        var response = await client.PatchAsync($"/1/devices/{deviceId}", TestHelper.JsonContent(new
        {
            name = "Hacked"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CrossUser_CannotDeleteOtherUsersDevices()
    {
        var user1 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "isodel1", "isodel1@test.org", "SecurePassword123#");
        var user2 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "isodel2", "isodel2@test.org", "SecurePassword123#");
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user1.Id, "ProtectedHub");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user2.SessionToken);

        var response = await client.DeleteAsync($"/1/devices/{deviceId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Cross-user isolation: tokens ---

    [Test]
    public async Task CrossUser_CannotSeeOtherUsersTokens()
    {
        var user1 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "isotok1", "isotok1@test.org", "SecurePassword123#");
        var user2 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "isotok2", "isotok2@test.org", "SecurePassword123#");

        // Create a token for user1 via authenticated client
        using var client1 = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user1.SessionToken);
        var createResponse = await client1.PostAsync("/1/tokens", TestHelper.JsonContent(new
        {
            name = "User1Token",
            permissions = new[] { "shockers.use" }
        }));
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = System.Text.Json.JsonDocument.Parse(createJson);
        var tokenId = createDoc.RootElement.GetProperty("id").GetString();

        // User2 tries to access it
        using var client2 = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user2.SessionToken);
        var response = await client2.GetAsync($"/1/tokens/{tokenId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Invalid session token ---

    [Test]
    public async Task InvalidSessionToken_Returns401()
    {
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, "totally-invalid-session-token-abc123");

        var response = await client.GetAsync("/1/devices");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
