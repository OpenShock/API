using System.Net;
using System.Text.Json;
using OpenShock.API.IntegrationTests.Helpers;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class UsersTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Get Self ---

    [Test]
    public async Task GetSelf_ReturnsCurrentUserInfo()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "userself", "userself@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync("/1/users/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetProperty("name").GetString()).IsEqualTo("userself");
        await Assert.That(data.GetProperty("email").GetString()).IsEqualTo("userself@test.org");
        await Assert.That(data.TryGetProperty("id", out _)).IsTrue();
        await Assert.That(data.TryGetProperty("image", out _)).IsTrue();
        await Assert.That(data.TryGetProperty("roles", out _)).IsTrue();
    }

    [Test]
    public async Task GetSelf_WithApiToken_ReturnsUserInfo()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "userselfapi", "userselfapi@test.org", "SecurePassword123#");
        var (_, rawToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, userId, "SelfApiToken");
        using var client = TestHelper.CreateApiTokenClient(WebApplicationFactory, rawToken);

        var response = await client.GetAsync("/1/users/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetProperty("name").GetString()).IsEqualTo("userselfapi");
    }

    // --- Lookup by Name ---

    [Test]
    public async Task LookupByName_Found_ReturnsUserInfo()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "lookupme", "lookupme@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync("/1/users/by-name/lookupme");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task LookupByName_NotFound_Returns404()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "lookupexist", "lookupexist@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync("/1/users/by-name/nonexistentuser12345");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Unauthenticated ---

    [Test]
    public async Task GetSelf_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1/users/self");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task LookupByName_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1/users/by-name/anyone");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
