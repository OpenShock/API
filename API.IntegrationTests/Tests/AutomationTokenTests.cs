using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.AutomationTokens;
using OpenShock.Common.Utils;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Automation tokens lift Turnstile (and rate limits, which the test server disables anyway) for any
/// account except privileged ones, so these tests use an invalid Turnstile response to observe whether
/// a token was honored.
/// </summary>
public sealed class AutomationTokenTests
{
    private const string Password = "SecurePassword123#";

    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    // --- Signup ---

    [Test]
    public async Task Signup_WithTurnstileToken_BypassesTurnstileAndLinksAccount()
    {
        var (tokenId, secret) = await CreateAutomationTokenInDb();
        using var client = CreateAutomationClient(secret);
        var email = TestHelper.UniqueEmail("autosignup");

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = TestHelper.UniqueUsername("autosignup"),
            password = Password,
            email,
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Email == email);
        await Assert.That(user.CreatedByAutomationTokenId).IsEqualTo(tokenId);
        await Assert.That(await HasUsedAuditAsync(db, user.Id)).IsTrue();
    }

    [Test]
    public async Task Signup_WithUnknownToken_StillRequiresTurnstile()
    {
        using var client = CreateAutomationClient(IAutomationTokenService.GenerateSecret());

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = TestHelper.UniqueUsername("autounknown"),
            password = Password,
            email = TestHelper.UniqueEmail("autounknown"),
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Signup_WithRateLimitOnlyToken_StillRequiresTurnstile()
    {
        var (_, secret) = await CreateAutomationTokenInDb([AutomationTokenType.RateLimit]);
        using var client = CreateAutomationClient(secret);

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = TestHelper.UniqueUsername("autoratelimit"),
            password = Password,
            email = TestHelper.UniqueEmail("autoratelimit"),
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    // --- Privileged callers (middleware) ---

    [Test]
    public async Task PrivilegedSession_TokenIsRefused()
    {
        var (_, secret) = await CreateAutomationTokenInDb();
        var staff = await CreateUserWithRoles("autostaffsession", [RoleType.Staff]);

        using var client = TestHelper.CreateAuthenticatedClient(WebApplicationFactory, staff.SessionToken);
        client.DefaultRequestHeaders.Add(AuthConstants.AutomationTokenHeaderName, secret);

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = TestHelper.UniqueUsername("autostaffsessionsignup"),
            password = Password,
            email = TestHelper.UniqueEmail("autostaffsessionsignup"),
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PrivilegedApiToken_TokenIsRefused()
    {
        var (_, secret) = await CreateAutomationTokenInDb();
        var staff = await CreateUserWithRoles("autostaffapi", [RoleType.Staff]);
        var (_, apiToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, staff.Id);

        using var client = TestHelper.CreateApiTokenClient(WebApplicationFactory, apiToken);
        client.DefaultRequestHeaders.Add(AuthConstants.AutomationTokenHeaderName, secret);

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = TestHelper.UniqueUsername("autostaffapisignup"),
            password = Password,
            email = TestHelper.UniqueEmail("autostaffapisignup"),
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task NormalApiToken_TokenIsHonored()
    {
        var (_, secret) = await CreateAutomationTokenInDb();
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("autonormalapi"), TestHelper.UniqueEmail("autonormalapi"), Password);
        var (_, apiToken) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, userId);

        using var client = TestHelper.CreateApiTokenClient(WebApplicationFactory, apiToken);
        client.DefaultRequestHeaders.Add(AuthConstants.AutomationTokenHeaderName, secret);

        var response = await client.PostAsync("/2/account/signup", TestHelper.JsonContent(new
        {
            username = TestHelper.UniqueUsername("autonormalapisignup"),
            password = Password,
            email = TestHelper.UniqueEmail("autonormalapisignup"),
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    // --- Login ---

    [Test]
    public async Task Login_ForNormalAccount_BypassesTurnstileAndAudits()
    {
        var (_, secret) = await CreateAutomationTokenInDb();
        var email = TestHelper.UniqueEmail("autologin");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("autologin"), email, Password);
        using var client = CreateAutomationClient(secret);

        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = email,
            password = Password,
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await HasUsedAuditAsync(db, userId)).IsTrue();
    }

    [Test]
    public async Task Login_ForPrivilegedAccount_Returns403()
    {
        var (_, secret) = await CreateAutomationTokenInDb();
        var admin = await CreateUserWithRoles("autologinadmin", [RoleType.Admin]);
        using var client = CreateAutomationClient(secret);

        var response = await client.PostAsync("/2/account/login", TestHelper.JsonContent(new
        {
            usernameOrEmail = admin.Email,
            password = Password,
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("AutomationToken.NotAllowedForAccount");
    }

    // --- Password reset ---

    [Test]
    public async Task PasswordReset_ForPrivilegedAccount_ReturnsOkWithoutStartingReset()
    {
        var (_, secret) = await CreateAutomationTokenInDb();
        var staff = await CreateUserWithRoles("autoresetstaff", [RoleType.Staff]);
        using var client = CreateAutomationClient(secret);

        var response = await client.PostAsync("/2/account/password-reset", TestHelper.JsonContent(new
        {
            email = staff.Email,
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.UserPasswordResets.AnyAsync(r => r.UserId == staff.Id)).IsFalse();
    }

    [Test]
    public async Task PasswordReset_ForNormalAccount_StartsResetAndAudits()
    {
        var (_, secret) = await CreateAutomationTokenInDb();
        var email = TestHelper.UniqueEmail("autoreset");
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("autoreset"), email, Password);
        using var client = CreateAutomationClient(secret);

        var response = await client.PostAsync("/2/account/password-reset", TestHelper.JsonContent(new
        {
            email,
            turnstileResponse = "invalid-token"
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.UserPasswordResets.AnyAsync(r => r.UserId == userId)).IsTrue();
        await Assert.That(await HasUsedAuditAsync(db, userId)).IsTrue();
    }

    // --- Admin endpoints ---

    [Test]
    public async Task Create_ReturnsWorkingSecret()
    {
        using var client = await CreateAdminClient("autocreate");

        var response = await client.PostAsync("/1/admin/automationTokens", TestHelper.JsonContent(new
        {
            name = "created-by-test",
            types = new[] { "Turnstile" }
        }));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetGuid();
        var secret = json.RootElement.GetProperty("secret").GetString()!;

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomationTokenService>();
        var resolved = await service.ResolveAsync(secret, CancellationToken.None);
        await Assert.That(resolved).IsNotNull();
        await Assert.That(resolved!.Id).IsEqualTo(id);
        await Assert.That(resolved.Types).Contains(AutomationTokenType.Turnstile);
    }

    [Test]
    public async Task Rotate_OldSecretStopsResolving()
    {
        var (tokenId, oldSecret) = await CreateAutomationTokenInDb();
        using var client = await CreateAdminClient("autorotate");

        var response = await client.PostAsync($"/1/admin/automationTokens/{tokenId}/rotate", null);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var newSecret = json.RootElement.GetProperty("secret").GetString()!;

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomationTokenService>();
        await Assert.That(await service.ResolveAsync(oldSecret, CancellationToken.None)).IsNull();
        await Assert.That((await service.ResolveAsync(newSecret, CancellationToken.None))?.Id).IsEqualTo(tokenId);
    }

    [Test]
    public async Task Patch_CleanupRequiresDelay_AndSwitchingOffClearsIt()
    {
        var (tokenId, _) = await CreateAutomationTokenInDb();
        using var client = await CreateAdminClient("autopatch");

        var missingDelay = await client.PatchAsync($"/1/admin/automationTokens/{tokenId}", TestHelper.JsonContent(new
        {
            autoCleanupUsers = true
        }));
        await Assert.That(missingDelay.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var enable = await client.PatchAsync($"/1/admin/automationTokens/{tokenId}", TestHelper.JsonContent(new
        {
            autoCleanupUsers = true,
            autoCleanupAfter = "01:00:00"
        }));
        await Assert.That(enable.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var disable = await client.PatchAsync($"/1/admin/automationTokens/{tokenId}", TestHelper.JsonContent(new
        {
            autoCleanupUsers = false
        }));
        await Assert.That(disable.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var token = await db.AutomationTokens.AsNoTracking().FirstAsync(t => t.Id == tokenId);
        await Assert.That(token.AutoCleanupUsers).IsFalse();
        await Assert.That(token.AutoCleanupAfter).IsNull();
    }

    [Test]
    public async Task Delete_WithPrivilegedCreatedAccount_Returns409AndKeepsEverything()
    {
        var (tokenId, _) = await CreateAutomationTokenInDb();
        var normalId = await CreateUserCreatedByToken(tokenId, "autodelnormal", []);
        var staffId = await CreateUserCreatedByToken(tokenId, "autodelstaff", [RoleType.Staff]);
        using var client = await CreateAdminClient("autodelete409");

        var response = await client.DeleteAsync($"/1/admin/automationTokens/{tokenId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.AutomationTokens.AnyAsync(t => t.Id == tokenId)).IsTrue();
        await Assert.That(await db.Users.AnyAsync(u => u.Id == normalId)).IsTrue();
        await Assert.That(await db.Users.AnyAsync(u => u.Id == staffId)).IsTrue();
    }

    [Test]
    public async Task Delete_DeletesTokenAndTheAccountsItCreated()
    {
        var (tokenId, _) = await CreateAutomationTokenInDb();
        var createdId = await CreateUserCreatedByToken(tokenId, "autodelcreated", []);
        var unrelatedId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername("autodelunrelated"), TestHelper.UniqueEmail("autodelunrelated"), Password);
        using var client = await CreateAdminClient("autodelete");

        var response = await client.DeleteAsync($"/1/admin/automationTokens/{tokenId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await Assert.That(await db.AutomationTokens.AnyAsync(t => t.Id == tokenId)).IsFalse();
        await Assert.That(await db.Users.AnyAsync(u => u.Id == createdId)).IsFalse();
        await Assert.That(await db.Users.AnyAsync(u => u.Id == unrelatedId)).IsTrue();
    }

    // --- Helpers ---

    private HttpClient CreateAutomationClient(string secret)
    {
        var client = WebApplicationFactory.CreateClient();
        client.DefaultRequestHeaders.Add(AuthConstants.AutomationTokenHeaderName, secret);
        return client;
    }

    private async Task<(Guid Id, string Secret)> CreateAutomationTokenInDb(List<AutomationTokenType>? types = null)
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var secret = IAutomationTokenService.GenerateSecret();
        var id = Guid.CreateVersion7();
        db.AutomationTokens.Add(new AutomationToken
        {
            Id = id,
            Name = "test-token",
            TokenHash = HashingUtils.HashToken(secret),
            Types = types ?? [AutomationTokenType.Turnstile, AutomationTokenType.RateLimit]
        });
        await db.SaveChangesAsync();
        return (id, secret);
    }

    private async Task<AuthenticatedUser> CreateUserWithRoles(string prefix, List<RoleType> roles)
    {
        var user = await TestHelper.CreateAndLoginUser(WebApplicationFactory, TestHelper.UniqueUsername(prefix), TestHelper.UniqueEmail(prefix), Password);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Roles, roles));

        return user;
    }

    private async Task<Guid> CreateUserCreatedByToken(Guid tokenId, string prefix, List<RoleType> roles)
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername(prefix), TestHelper.UniqueEmail(prefix), Password);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s
            .SetProperty(u => u.CreatedByAutomationTokenId, tokenId)
            .SetProperty(u => u.Roles, roles));

        return userId;
    }

    private async Task<HttpClient> CreateAdminClient(string prefix)
    {
        var admin = await CreateUserWithRoles(prefix, [RoleType.Admin]);
        return TestHelper.CreateAuthenticatedClient(WebApplicationFactory, admin.SessionToken);
    }

    private static Task<bool> HasUsedAuditAsync(OpenShockContext db, Guid userId) =>
        db.UserAuditLogs.AnyAsync(l => l.UserId == userId && l.Action == AuditAction.AutomationTokenUsed);
}
