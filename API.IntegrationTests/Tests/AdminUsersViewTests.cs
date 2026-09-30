using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.OpenShockDb;

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
}
