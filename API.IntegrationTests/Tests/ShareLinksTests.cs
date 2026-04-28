using System.Net;
using System.Text.Json;
using OpenShock.API.IntegrationTests.Helpers;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class ShareLinksTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- List share links ---

    [Test]
    public async Task ListShareLinks_Empty_ReturnsEmptyArray()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sharelinklist", "sharelinklist@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.GetAsync("/1/shares/links");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetArrayLength()).IsEqualTo(0);
    }

    // --- Create share link ---

    [Test]
    public async Task CreateShareLink_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sharelinkcreat", "sharelinkcreat@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/shares/links", TestHelper.JsonContent(new
        {
            name = "My Public Share"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(Guid.TryParse(data.GetString(), out _)).IsTrue();
    }

    [Test]
    public async Task CreateShareLink_ThenListContainsIt()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sharelinkcrls", "sharelinkcrls@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        await client.PostAsync("/1/shares/links", TestHelper.JsonContent(new
        {
            name = "Test Share"
        }));

        var response = await client.GetAsync("/1/shares/links");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        await Assert.That(data.GetArrayLength()).IsGreaterThanOrEqualTo(1);
    }

    // --- Delete share link ---

    [Test]
    public async Task DeleteShareLink_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sharelinkdel", "sharelinkdel@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var createResp = await client.PostAsync("/1/shares/links", TestHelper.JsonContent(new
        {
            name = "To Delete"
        }));
        var createJson = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var shareId = createDoc.RootElement.GetProperty("data").GetString();

        var response = await client.DeleteAsync($"/1/shares/links/{shareId}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DeleteShareLink_NotFound()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sharelinkdnf", "sharelinkdnf@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.DeleteAsync($"/1/shares/links/{Guid.NewGuid()}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // --- Add shocker to share link ---

    [Test]
    public async Task AddShockerToShareLink_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sharelinkshock", "sharelinkshock@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        // Create device and shocker via DB helpers
        var (deviceId, _) = await TestHelper.CreateDeviceInDb(WebApplicationFactory, user.Id, "ShareDevice");

        var shockerResp = await client.PostAsync("/1/shockers", TestHelper.JsonContent(new
        {
            name = "ShareShocker",
            rfId = 12345,
            model = 0,
            device = deviceId
        }));
        await Assert.That(shockerResp.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var shockerJson = await shockerResp.Content.ReadAsStringAsync();
        using var shockerDoc = JsonDocument.Parse(shockerJson);
        var shockerId = shockerDoc.RootElement.GetProperty("data").GetString();

        // Create public share
        var createResp = await client.PostAsync("/1/shares/links", TestHelper.JsonContent(new
        {
            name = "Share With Shocker"
        }));
        await Assert.That(createResp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var createJson = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var shareId = createDoc.RootElement.GetProperty("data").GetString();

        // Add shocker to share
        var response = await client.PostAsync($"/1/shares/links/{shareId}/{shockerId}", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    // --- Unauthenticated access ---

    [Test]
    public async Task ListShareLinks_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/1/shares/links");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- Cross-user isolation ---

    [Test]
    public async Task DeleteShareLink_OtherUser_Returns404()
    {
        var user1 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sharelinkown", "sharelinkown@test.org", "SecurePassword123#");
        var user2 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "sharelinkoth", "sharelinkoth@test.org", "SecurePassword123#");

        using var client1 = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user1.SessionToken);
        using var client2 = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user2.SessionToken);

        var createResp = await client1.PostAsync("/1/shares/links", TestHelper.JsonContent(new
        {
            name = "User1's Share"
        }));
        var createJson = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var shareId = createDoc.RootElement.GetProperty("data").GetString();

        // User2 tries to delete user1's share
        var response = await client2.DeleteAsync($"/1/shares/links/{shareId}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
