using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Cron.IntegrationTests.Tests;

/// <summary>
/// Runs the delivery job's actual claim query (<see cref="EmailOutboxQueries.DueForDelivery"/>, the single
/// source of truth the job uses) against real Postgres via the Cron host's own pooled context - the same
/// registration + enum-mapping path that runs in production. Guards the raw SQL (table/column names, the
/// email_status enum, LIMIT, FOR UPDATE SKIP LOCKED, SELECT * materialization) so a schema rename breaks
/// this test rather than only surfacing in the Cron host at runtime.
/// </summary>
// Serialized with EmailOutboxDeliveryTests: both share the session Postgres, and the delivery job
// claims due rows under FOR UPDATE (oldest-first). Running concurrently, that lock makes this test's
// FOR UPDATE SKIP LOCKED read skip its own (oldest) row. The shared key removes that window.
[NotInParallel("email-outbox")]
public sealed class EmailOutboxQueryTests
{
    [ClassDataSource<CronApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required CronApplicationFactory Factory { get; init; }

    [Test]
    public async Task DueForDelivery_RunsTheJobsRawSql_AgainstTheRealSchema()
    {
        var recipient = $"outbox-claim-sql-{Guid.CreateVersion7():N}@test.org";

        Guid id;
        await using (var db = await Factory.DbContextFactory.CreateDbContextAsync())
        {
            var message = EmailOutboxMessage.Create(
                EmailType.PasswordReset, recipient, "Claim",
                new Dictionary<string, string> { ["k"] = "v" });
            // Dated well into the past so it sorts first and the global LIMIT can never exclude it.
            message.NextAttemptAt = DateTime.UtcNow - TimeSpan.FromDays(1);
            db.EmailOutbox.Add(message);
            await db.SaveChangesAsync();
            id = message.Id;
        }

        // Resolve the pooled OpenShockContext exactly as the Cron delivery job does (DI-injected) so this
        // covers the same registration + enum-mapping path that runs in production.
        using var scope = Factory.Services.CreateScope();
        var db2 = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        var claimed = await db2.EmailOutbox.DueForDelivery(1).ToListAsync();

        await Assert.That(claimed.Any(m => m.Id == id)).IsTrue();
    }
}
