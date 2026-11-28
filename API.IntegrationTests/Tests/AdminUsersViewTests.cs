using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Session;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Covers the <c>admin_users_view.password_hash_type</c> mapping. The column is plain text derived
/// from the <c>bcrypt:</c>/<c>pbkdf2:</c> prefix on <c>users.password_hash</c> (not the legacy
/// <c>password_encryption_type</c> Postgres enum), so EF converts it to
/// <see cref="PasswordHashingAlgorithm"/> by matching enum member names case-insensitively. That
/// coupling between the hash prefix and the enum member name is invisible at the call site, hence
/// these tests.
/// </summary>
public sealed class AdminUsersViewTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task PasswordHashType_ForPasswordUser_MapsToBCrypt()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "adminviewbcrypt", "adminviewbcrypt@test.org", "SecurePassword123#");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var hash = await db.Users.Where(u => u.Id == userId).Select(u => u.PasswordHash).FirstAsync();
        await Assert.That(hash).StartsWith("bcrypt:");

        var view = await db.AdminUsersViews.AsNoTracking().FirstAsync(v => v.Id == userId);
        await Assert.That(view.PasswordHashType).IsEqualTo(PasswordHashingAlgorithm.BCrypt);
    }

    [Test]
    public async Task PasswordHashType_ForOAuthOnlyUser_IsNull()
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var userId = Guid.CreateVersion7();
        db.Users.Add(new User
        {
            Id = userId,
            Name = "adminviewoauth",
            Email = "adminviewoauth@test.org",
            PasswordHash = null,
            ActivatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var view = await db.AdminUsersViews.AsNoTracking().FirstAsync(v => v.Id == userId);
        await Assert.That(view.PasswordHashType).IsNull();
    }

    [Test]
    public async Task Roles_ForUserWithoutRoles_MaterializesAsEmptyList()
    {
        var userId = await CreateUserWithRolesAsync(TestHelper.UniqueUsername("adminviewnoroles"), []);

        await using var readScope = WebApplicationFactory.Services.CreateAsyncScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var view = await readDb.AdminUsersViews.AsNoTracking().FirstAsync(v => v.Id == userId);
        await Assert.That(view.Roles).IsEmpty();
    }

    [Test]
    public async Task Roles_ForUserWithRoles_MaterializesRoles()
    {
        List<RoleType> expectedRoles = [RoleType.Support, RoleType.Staff, RoleType.Admin];
        var userId = await CreateUserWithRolesAsync(TestHelper.UniqueUsername("adminviewroles"), expectedRoles);

        await using var readScope = WebApplicationFactory.Services.CreateAsyncScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var view = await readDb.AdminUsersViews.AsNoTracking().FirstAsync(v => v.Id == userId);
        await Assert.That(view.Roles).IsEquivalentTo(expectedRoles);
    }

    [Test]
    public async Task GetUsersEndpoint_WithFilterOrderByAndPagination_MaterializesRolesThroughFuture()
    {
        using var adminClient = await CreateAdminClientAsync("adminviewhttp");
        var prefix = $"admlist{Guid.CreateVersion7():N}"[..16];

        var userAId = await CreateUserWithRolesAsync($"{prefix}a", []);
        var userBId = await CreateUserWithRolesAsync($"{prefix}b", [RoleType.Support, RoleType.Admin]);
        _ = await CreateUserWithRolesAsync($"{prefix}c", [RoleType.Staff]);

        var filter = Uri.EscapeDataString($"name ilike '{prefix}%'");
        var orderBy = Uri.EscapeDataString("name desc");
        var response = await adminClient.GetAsync($"/1/admin/users?$filter={filter}&$orderby={orderBy}&$offset=1&$limit=2");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<Paginated<AdminUsersView>>(json, JsonOptions.Default);

        await Assert.That(page).IsNotNull();
        await Assert.That(page!.Total).IsEqualTo(3);
        await Assert.That(page.Offset).IsEqualTo(1);
        await Assert.That(page.Limit).IsEqualTo(2);
        await Assert.That(page.Data.Count).IsEqualTo(2);

        await Assert.That(page.Data[0].Id).IsEqualTo(userBId);
        await Assert.That(page.Data[0].Roles).IsEquivalentTo([RoleType.Support, RoleType.Admin]);

        await Assert.That(page.Data[1].Id).IsEqualTo(userAId);
        await Assert.That(page.Data[1].Roles).IsEmpty();
    }

    [Test]
    public async Task GetUsersEndpoint_WithoutFilter_MaterializesDefaultOrderedPage()
    {
        using var adminClient = await CreateAdminClientAsync("adminviewdefault");

        var response = await adminClient.GetAsync("/1/admin/users?$limit=10");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<Paginated<AdminUsersView>>(json, JsonOptions.Default);

        await Assert.That(page).IsNotNull();
        await Assert.That(page!.Total).IsGreaterThanOrEqualTo(1);
        await Assert.That(page.Data).IsNotEmpty();
    }

    private async Task<HttpClient> CreateAdminClientAsync(string prefix)
    {
        var adminId = await CreateUserWithRolesAsync(TestHelper.UniqueUsername(prefix), [RoleType.Admin]);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var session = await sessionService.CreateSessionAsync(adminId, "IntegrationTest", IPAddress.Loopback, actorId: adminId);

        return TestHelper.CreateAuthenticatedClient(WebApplicationFactory, session.Token);
    }

    private async Task<Guid> CreateUserWithRolesAsync(string username, List<RoleType> roles)
    {
        var userId = Guid.CreateVersion7();

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        db.Users.Add(new User
        {
            Id = userId,
            Name = username,
            Email = TestHelper.UniqueEmail(username),
            PasswordHash = null,
            ActivatedAt = DateTime.UtcNow,
            Roles = roles
        });
        await db.SaveChangesAsync();

        return userId;
    }
}
