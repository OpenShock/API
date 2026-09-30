using System.Net;
using OpenShock.API.IntegrationTests.Helpers;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class AccountAuthenticatedTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Change Password ---

    [Test]
    public async Task ChangePassword_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "chgpwd", "chgpwd@test.org", "OldPassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/password", TestHelper.JsonContent(new
        {
            currentPassword = "OldPassword123#",
            newPassword = "NewPassword456#"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify can login with new password
        using var loginClient = WebApplicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        var loginResponse = await loginClient.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = "chgpwd@test.org",
            password = "NewPassword456#",
            turnstileResponse = "valid-token"
        }));
        await Assert.That(loginResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ChangePassword_WrongCurrentPassword_Returns403()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "chgpwdbad", "chgpwdbad@test.org", "CorrectPassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/password", TestHelper.JsonContent(new
        {
            currentPassword = "WrongPassword!",
            newPassword = "NewPassword456#"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    // --- Change Username ---

    [Test]
    public async Task ChangeUsername_Success()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "oldname", "chguname@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/username", TestHelper.JsonContent(new
        {
            username = "newname"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ChangeUsername_Taken_Returns409()
    {
        await TestHelper.CreateAndLoginUser(WebApplicationFactory, "takenname", "takenname@test.org", "SecurePassword123#");
        var user2 = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "wantsname", "wantsname@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user2.SessionToken);

        var response = await client.PostAsync("/1/account/username", TestHelper.JsonContent(new
        {
            username = "takenname"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    // --- Unauthenticated access ---

    [Test]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/account/password", TestHelper.JsonContent(new
        {
            currentPassword = "anything",
            newPassword = "anything"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ChangeUsername_Unauthenticated_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/account/username", TestHelper.JsonContent(new
        {
            username = "anything"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
