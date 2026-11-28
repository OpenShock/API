using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using OpenShock.Cron.Jobs;

namespace OpenShock.Cron.IntegrationTests.Tests;

/// <summary>
/// Runs the unactivated-account cleanup against real Postgres, so the raw delete's <c>role_type[]</c>
/// parameter and the deactivation sub-select are exercised.
/// </summary>
public sealed class DeleteUnactivatedAccountsTests
{
    [ClassDataSource<CronApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required CronApplicationFactory Factory { get; init; }

    [Test]
    public async Task Execute_DeletesOnlyStaleUnactivatedAccounts()
    {
        var stale = DateTime.UtcNow - Duration.UnactivatedAccountRetentionTime - TimeSpan.FromHours(1);
        var recent = DateTime.UtcNow - TimeSpan.FromHours(1);

        var staleUnactivated = Guid.CreateVersion7();
        var recentUnactivated = Guid.CreateVersion7();
        var staleActivated = Guid.CreateVersion7();
        var staleUnactivatedStaff = Guid.CreateVersion7();
        var staleUnactivatedDeactivated = Guid.CreateVersion7();

        await using (var db = await Factory.DbContextFactory.CreateDbContextAsync())
        {
            var pending = NewUser(staleUnactivated, stale, activated: false, []);
            pending.UserActivationRequest = new UserActivationRequest
            {
                UserId = staleUnactivated,
                TokenHash = HashingUtils.HashToken(Guid.NewGuid().ToString("N"))
            };
            db.Users.Add(pending);
            db.Users.Add(NewUser(recentUnactivated, recent, activated: false, []));
            db.Users.Add(NewUser(staleActivated, stale, activated: true, []));
            db.Users.Add(NewUser(staleUnactivatedStaff, stale, activated: false, [RoleType.Staff]));

            var deactivated = NewUser(staleUnactivatedDeactivated, stale, activated: false, []);
            deactivated.UserDeactivation = new UserDeactivation
            {
                DeactivatedUserId = staleUnactivatedDeactivated,
                DeactivatedByUserId = staleUnactivatedDeactivated,
                DeleteLater = false
            };
            db.Users.Add(deactivated);

            await db.SaveChangesAsync();
        }

        await using var scope = Factory.Services.CreateAsyncScope();
        var job = ActivatorUtilities.CreateInstance<DeleteUnactivatedAccountsJob>(scope.ServiceProvider);

        await job.Execute();

        await using var verify = await Factory.DbContextFactory.CreateDbContextAsync();
        var remaining = await verify.Users
            .Where(u => new[] { staleUnactivated, recentUnactivated, staleActivated, staleUnactivatedStaff, staleUnactivatedDeactivated }.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        await Assert.That(remaining).DoesNotContain(staleUnactivated);
        await Assert.That(remaining).Contains(recentUnactivated);
        await Assert.That(remaining).Contains(staleActivated);
        await Assert.That(remaining).Contains(staleUnactivatedStaff);
        await Assert.That(remaining).Contains(staleUnactivatedDeactivated);

        // The activation link dies with the account.
        await Assert.That(await verify.UserActivationRequests.AnyAsync(r => r.UserId == staleUnactivated)).IsFalse();
    }

    private static User NewUser(Guid id, DateTime createdAt, bool activated, List<RoleType> roles)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return new User
        {
            Id = id,
            Name = $"unact{suffix}",
            Email = $"unact-{suffix}@test.org",
            SecurityStamp = Guid.CreateVersion7(),
            Roles = roles,
            CreatedAt = createdAt,
            ActivatedAt = activated ? createdAt : null
        };
    }
}
