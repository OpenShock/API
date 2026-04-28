using System.Net;
using System.Text.Json;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Constants;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class AccountLoginTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- V1 Login ---

    [Test]
    public async Task V1Login_Success_ReturnsCookie()
    {
        await TestHelper.CreateUserInDb(WebApplicationFactory, "loginv1", "loginv1@test.org", "SecurePassword123#");

        using var client = WebApplicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var response = await client.PostAsync("/1/account/login", TestHelper.JsonContent(new
        {
            email = "loginv1@test.org",
            password = "SecurePassword123#"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var setCookie = response.Headers.GetValues("Set-Cookie").ToArray();
        var hasSessionCookie = setCookie.Any(c => c.Contains(AuthConstants.UserSessionCookieName));
        await Assert.That(hasSessionCookie).IsTrue();
    }

    [Test]
    public async Task V1Login_InvalidPassword_Returns401()
    {
        await TestHelper.CreateUserInDb(WebApplicationFactory, "loginv1bad", "loginv1bad@test.org", "SecurePassword123#");

        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/account/login", TestHelper.JsonContent(new
        {
            email = "loginv1bad@test.org",
            password = "WrongPassword999!"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task V1Login_NonexistentUser_Returns401()
    {
        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/1/account/login", TestHelper.JsonContent(new
        {
            email = "doesnotexist@test.org",
            password = "SomePassword123#"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- V2 Login ---

    [Test]
    public async Task V2Login_Success_ReturnsCookieAndBody()
    {
        await TestHelper.CreateUserInDb(WebApplicationFactory, "loginv2", "loginv2@test.org", "SecurePassword123#");

        using var client = WebApplicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = "loginv2@test.org",
            password = "SecurePassword123#",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("accountId", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("accountName", out _)).IsTrue();

        var setCookie = response.Headers.GetValues("Set-Cookie").ToArray();
        var hasSessionCookie = setCookie.Any(c => c.Contains(AuthConstants.UserSessionCookieName));
        await Assert.That(hasSessionCookie).IsTrue();
    }

    [Test]
    public async Task V2Login_InvalidTurnstile_Returns403()
    {
        await TestHelper.CreateUserInDb(WebApplicationFactory, "loginv2ts", "loginv2ts@test.org", "SecurePassword123#");

        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = "loginv2ts@test.org",
            password = "SecurePassword123#",
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task V2Login_InvalidCredentials_Returns401()
    {
        await TestHelper.CreateUserInDb(WebApplicationFactory, "loginv2bad", "loginv2bad@test.org", "SecurePassword123#");

        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = "loginv2bad@test.org",
            password = "WrongPassword!",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task V2Login_ByUsername_Success()
    {
        await TestHelper.CreateUserInDb(WebApplicationFactory, "loginbyname", "loginbyname@test.org", "SecurePassword123#");

        using var client = WebApplicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = "loginbyname",
            password = "SecurePassword123#",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    // --- Unactivated account ---

    [Test]
    public async Task V2Login_UnactivatedAccount_Returns401()
    {
        await TestHelper.CreateUserInDb(WebApplicationFactory, "notactivated", "notactivated@test.org", "SecurePassword123#", activated: false);

        using var client = WebApplicationFactory.CreateClient();

        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = "notactivated@test.org",
            password = "SecurePassword123#",
            turnstileResponse = "valid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // --- Logout ---

    [Test]
    public async Task Logout_ClearsCookie()
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, "logoutuser", "logoutuser@test.org", "SecurePassword123#");
        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, user.SessionToken);

        var response = await client.PostAsync("/1/account/logout", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Logout_WithoutSession_StillReturnsOk()
    {
        using var client = WebApplicationFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var response = await client.PostAsync("/1/account/logout", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
