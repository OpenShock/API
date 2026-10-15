using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.API.Services.Account;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Results;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Moderation actions check the executing user's roles in the database, which Npgsql can only do by
/// reading the roles array: predicates over it (<c>Roles.Any</c>, <c>SelectMany(u => u.Roles)</c>) can't be
/// translated. These run the real queries so such a regression throws here.
/// </summary>
public sealed class AccountModerationTests
{
    private const string Password = "SecurePassword123#";

    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task Deactivate_ByStaff_Succeeds()
    {
        var staffId = await CreateUser("modstaffdeact", [RoleType.Staff]);
        var targetId = await CreateUser("modtargetdeact", []);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var result = await accountService.DeactivateAccountAsync(staffId, targetId, deleteLater: false);

        await Assert.That(result.Value is Success).IsTrue();
    }

    [Test]
    public async Task Deactivate_ByNormalUser_IsUnauthorized()
    {
        var userId = await CreateUser("modnormaldeact", []);
        var targetId = await CreateUser("modtargetnormal", []);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var result = await accountService.DeactivateAccountAsync(userId, targetId, deleteLater: false);

        await Assert.That(result.Value is Unauthorized).IsTrue();
    }

    [Test]
    public async Task Reactivate_ByStaff_Succeeds()
    {
        var staffId = await CreateUser("modstaffreact", [RoleType.Staff]);
        var targetId = await CreateUser("modtargetreact", []);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var deactivated = await accountService.DeactivateAccountAsync(staffId, targetId, deleteLater: false);
        await Assert.That(deactivated.Value is Success).IsTrue();

        var result = await accountService.ReactivateAccountAsync(staffId, targetId);

        await Assert.That(result.Value is Success).IsTrue();
    }

    [Test]
    public async Task Delete_ByAdmin_Succeeds()
    {
        var adminId = await CreateUser("modadmindel", [RoleType.Admin]);
        var targetId = await CreateUser("modtargetdel", []);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var result = await accountService.DeleteAccountAsync(adminId, targetId);

        await Assert.That(result.Value is Success).IsTrue();
    }

    [Test]
    public async Task Delete_ByNormalUser_IsUnauthorized()
    {
        var userId = await CreateUser("modnormaldel", []);
        var targetId = await CreateUser("modtargetnormdel", []);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var result = await accountService.DeleteAccountAsync(userId, targetId);

        await Assert.That(result.Value is Unauthorized).IsTrue();
    }

    private async Task<Guid> CreateUser(string prefix, List<RoleType> roles)
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, TestHelper.UniqueUsername(prefix), TestHelper.UniqueEmail(prefix), Password);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Roles, roles));

        return userId;
    }
}
