using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Covers behaviour that lives in the database rather than in C#: the case-insensitive email collation, the
/// username blacklist query, the native audit log column types and the check constraints.
/// </summary>
public sealed class DatabaseConstraintTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task Email_LookupIgnoresCase()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "dbemaillookup", "dbemaillookup@test.org", "SecurePassword123#");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var found = await db.Users.Where(u => u.Email == "DbEmailLookup@Test.ORG").Select(u => u.Id).ToListAsync();

        await Assert.That(found).IsEquivalentTo([userId]);
    }

    [Test]
    public async Task Email_UniquenessIgnoresCase()
    {
        await TestHelper.CreateUserInDb(WebApplicationFactory, "dbemailunique", "dbemailunique@test.org", "SecurePassword123#");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        db.Users.Add(new User
        {
            Name = "dbemailunique2",
            Email = "DBEMAILUNIQUE@test.org",
            PasswordHash = null,
            ActivatedAt = DateTime.UtcNow
        });

        var exception = await Assert.That(() => db.SaveChangesAsync()).Throws<DbUpdateException>();
        await Assert.That((exception!.InnerException as PostgresException)?.SqlState).IsEqualTo(PostgresErrorCodes.UniqueViolation);
    }

    [Test]
    public async Task UserNameBlacklist_MatchesExactAndContainedEntries()
    {
        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var exact = new UserNameBlacklist { Value = "ZqxExactEntry", MatchType = MatchTypeEnum.Exact };
        var contains = new UserNameBlacklist { Value = "Zqx_Part", MatchType = MatchTypeEnum.Contains };
        db.UserNameBlacklists.AddRange(exact, contains);
        await db.SaveChangesAsync();

        try
        {
            async Task<Guid[]> Matching(string username) =>
                await db.UserNameBlacklists.Matching(username).Select(x => x.Id).ToArrayAsync();

            await Assert.That(await Matching("zqxexactentry")).IsEquivalentTo([exact.Id]);
            // An exact entry is not a substring match
            await Assert.That(await Matching("xzqxexactentryx")).IsEmpty();
            await Assert.That(await Matching("some_ZQX_PART_name")).IsEquivalentTo([contains.Id]);
            // The underscore in the entry is literal, not a single character wildcard
            await Assert.That(await Matching("somezqxxpartname")).IsEmpty();
        }
        finally
        {
            await db.UserNameBlacklists.Where(x => x.Id == exact.Id || x.Id == contains.Id).ExecuteDeleteAsync();
        }
    }

    [Test]
    public async Task AuditLog_RoundTripsIpAddressAndMetadata()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "dbauditroundtrip", "dbauditroundtrip@test.org", "SecurePassword123#");
        var sessionId = Guid.CreateVersion7();
        var ipAddress = IPAddress.Parse("2001:db8::1");

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var entry = new UserAuditLog
        {
            UserId = userId,
            ActorId = userId,
            Action = AuditAction.Login,
            IpAddress = ipAddress,
            Metadata = new LoginMetadata(sessionId),
            CreatedAt = DateTime.UtcNow
        };
        db.UserAuditLogs.Add(entry);
        await db.SaveChangesAsync();

        var stored = await db.UserAuditLogs.AsNoTracking().FirstAsync(x => x.Id == entry.Id);

        await Assert.That(stored.IpAddress).IsEqualTo(ipAddress);
        await Assert.That(stored.Metadata).IsEqualTo(new LoginMetadata(sessionId));
    }

    [Test]
    public async Task ApiToken_RejectsControlLimitsOutOfRange()
    {
        var userId = await TestHelper.CreateUserInDb(WebApplicationFactory, "dbtokenlimits", "dbtokenlimits@test.org", "SecurePassword123#");
        var (tokenId, _) = await TestHelper.CreateApiTokenInDb(WebApplicationFactory, userId);

        await using var scope = WebApplicationFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var aboveMaximum = await Assert.That(() => db.ApiTokens
                .Where(t => t.Id == tokenId)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.ShockerControlIntensityMax, (byte)101)))
            .Throws<PostgresException>();
        await Assert.That(aboveMaximum!.SqlState).IsEqualTo(PostgresErrorCodes.CheckViolation);

        var minimumAboveMaximum = await Assert.That(() => db.ApiTokens
                .Where(t => t.Id == tokenId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.ShockerControlIntensityMin, (byte)60)
                    .SetProperty(t => t.ShockerControlIntensityMax, (byte)40)))
            .Throws<PostgresException>();
        await Assert.That(minimumAboveMaximum!.SqlState).IsEqualTo(PostgresErrorCodes.CheckViolation);
    }
}
