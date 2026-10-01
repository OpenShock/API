using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using OpenShock.Cron.Jobs;

namespace OpenShock.Cron.IntegrationTests.Tests;

/// <summary>
/// Runs the automated-account cleanup against real Postgres, so the interval arithmetic, the join to the
/// token and the raw delete's <c>role_type[]</c> parameter are exercised, not just the LINQ shape.
/// </summary>
public sealed class DeleteExpiredAutomatedAccountsTests
{
    [ClassDataSource<CronApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required CronApplicationFactory Factory { get; init; }

    [Test]
    public async Task Execute_DeletesOnlyExpiredNonPrivilegedAccountsOfCleanupTokens()
    {
        var cleanupToken = Guid.CreateVersion7();
        var keepToken = Guid.CreateVersion7();
        var longAgo = DateTime.UtcNow - TimeSpan.FromHours(2);

        var expired = Guid.CreateVersion7();
        var expiredStaff = Guid.CreateVersion7();
        var fresh = Guid.CreateVersion7();
        var expiredOfKeepToken = Guid.CreateVersion7();
        var expiredUnlinked = Guid.CreateVersion7();

        await using (var db = await Factory.DbContextFactory.CreateDbContextAsync())
        {
            db.AutomationTokens.Add(NewToken(cleanupToken, autoCleanup: true));
            db.AutomationTokens.Add(NewToken(keepToken, autoCleanup: false));

            db.Users.Add(NewUser(expired, longAgo, cleanupToken, []));
            db.Users.Add(NewUser(expiredStaff, longAgo, cleanupToken, [RoleType.Staff]));
            db.Users.Add(NewUser(fresh, DateTime.UtcNow, cleanupToken, []));
            db.Users.Add(NewUser(expiredOfKeepToken, longAgo, keepToken, []));
            db.Users.Add(NewUser(expiredUnlinked, longAgo, null, []));

            await db.SaveChangesAsync();
        }

        await using var scope = Factory.Services.CreateAsyncScope();
        var job = ActivatorUtilities.CreateInstance<DeleteExpiredAutomatedAccountsJob>(scope.ServiceProvider);

        await job.Execute();

        await using var verify = await Factory.DbContextFactory.CreateDbContextAsync();
        var remaining = await verify.Users
            .Where(u => new[] { expired, expiredStaff, fresh, expiredOfKeepToken, expiredUnlinked }.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        await Assert.That(remaining).DoesNotContain(expired);
        await Assert.That(remaining).Contains(expiredStaff);
        await Assert.That(remaining).Contains(fresh);
        await Assert.That(remaining).Contains(expiredOfKeepToken);
        await Assert.That(remaining).Contains(expiredUnlinked);
    }

    private static AutomationToken NewToken(Guid id, bool autoCleanup) => new()
    {
        Id = id,
        Name = "cleanup-test",
        TokenHash = HashingUtils.HashToken(Guid.NewGuid().ToString("N")),
        Types = [AutomationTokenType.Turnstile],
        AutoCleanupUsers = autoCleanup,
        AutoCleanupAfter = TimeSpan.FromHours(1)
    };

    private static User NewUser(Guid id, DateTime createdAt, Guid? tokenId, List<RoleType> roles)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return new User
        {
            Id = id,
            Name = $"auto{suffix}",
            Email = $"auto-{suffix}@test.org",
            SecurityStamp = Guid.CreateVersion7(),
            Roles = roles,
            CreatedAt = createdAt,
            ActivatedAt = createdAt,
            CreatedByAutomationTokenId = tokenId
        };
    }
}
