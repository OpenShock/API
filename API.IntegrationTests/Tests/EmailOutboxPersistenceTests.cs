using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.IntegrationTests.Tests;

/// <summary>
/// Database round-trip coverage for the email_outbox row against real Postgres: that every
/// <see cref="EmailStatus"/> (including the new <c>sending</c>/<c>skipped</c> enum values) and the new
/// attempt/next-attempt columns persist and read back, and that the delivery job's claim predicate
/// (the enum filter used by the FOR UPDATE SKIP LOCKED query) selects the right rows. The pure
/// transition logic is covered by the Cron unit tests; this guards the persistence mapping the unit
/// tests can't.
/// </summary>
public sealed class EmailOutboxPersistenceTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task EveryStatus_AndAttemptColumns_RoundTripThroughPostgres()
    {
        var factory = WebApplicationFactory.Services.GetRequiredService<IDbContextFactory<OpenShockContext>>();

        foreach (var status in Enum.GetValues<EmailStatus>())
        {
            Guid id;
            await using (var db = await factory.CreateDbContextAsync())
            {
                var message = EmailOutboxMessage.Create(
                    EmailType.PasswordReset, TestHelper.UniqueEmail("outbox-persist"), "Persist",
                    new Dictionary<string, string> { ["k"] = "v" });
                message.Status = status;
                message.AttemptCount = 3;
                message.LastError = "boom";
                message.CoalesceKey = "pwreset:round-trip";
                db.EmailOutbox.Add(message);
                await db.SaveChangesAsync();
                id = message.Id;
            }

            await using var verifyDb = await factory.CreateDbContextAsync();
            var loaded = await verifyDb.EmailOutbox.AsNoTracking().FirstAsync(m => m.Id == id);

            await Assert.That(loaded.Status).IsEqualTo(status);
            await Assert.That(loaded.AttemptCount).IsEqualTo(3);
            await Assert.That(loaded.LastError).IsEqualTo("boom");
            await Assert.That(loaded.CoalesceKey).IsEqualTo("pwreset:round-trip");
        }
    }

    [Test]
    public async Task ClaimPredicate_SelectsDuePendingAndLapsedSending_NotTerminalOrFuture()
    {
        var factory = WebApplicationFactory.Services.GetRequiredService<IDbContextFactory<OpenShockContext>>();
        var recipient = TestHelper.UniqueEmail("outbox-claim");
        var past = DateTime.UtcNow - TimeSpan.FromMinutes(5);
        var future = DateTime.UtcNow + TimeSpan.FromHours(1);

        var duePending = await SeedAsync(factory, recipient, EmailStatus.Pending, past);
        var lapsedSending = await SeedAsync(factory, recipient, EmailStatus.Sending, past);
        await SeedAsync(factory, recipient, EmailStatus.Pending, future);   // scheduled retry, not yet due
        await SeedAsync(factory, recipient, EmailStatus.Sent, past);        // terminal

        await using var db = await factory.CreateDbContextAsync();

        // The delivery job's claim predicate (enum filter + dueness), scoped to this test's recipient so
        // it never touches rows from other tests and needs no FOR UPDATE.
        var claimed = await db.EmailOutbox
            .FromSql(
                $"""
                 SELECT * FROM email_outbox
                 WHERE recipient = {recipient}
                   AND next_attempt_at <= now()
                   AND (status = {EmailStatus.Pending} OR status = {EmailStatus.Sending})
                 """)
            .AsNoTracking()
            .Select(m => m.Id)
            .ToListAsync();

        await Assert.That(claimed).Contains(duePending);
        await Assert.That(claimed).Contains(lapsedSending);
        await Assert.That(claimed.Count).IsEqualTo(2);
    }

    [Test]
    public async Task DueForDelivery_RunsTheJobsRawSql_AgainstTheRealSchema()
    {
        var factory = WebApplicationFactory.Services.GetRequiredService<IDbContextFactory<OpenShockContext>>();
        var recipient = TestHelper.UniqueEmail("outbox-claim-sql");

        // Dated well into the past so it sorts first and the global LIMIT can never exclude it.
        var id = await SeedAsync(factory, recipient, EmailStatus.Pending, DateTime.UtcNow - TimeSpan.FromDays(1));

        // Resolve the pooled OpenShockContext exactly as the Cron delivery job does (DI-injected, not the
        // factory) so this covers the same registration + enum-mapping path that runs in production.
        using var scope = WebApplicationFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();

        // Runs the delivery job's *actual* claim query (EmailOutboxQueries.DueForDelivery, the single source
        // of truth the job uses) against real Postgres: the email_outbox table name, its column names, the
        // email_status enum, LIMIT, FOR UPDATE SKIP LOCKED, and SELECT * -> full entity materialization
        // (jsonb payload included). A schema rename breaks this test rather than only the Cron host at runtime.
        var claimed = await db.EmailOutbox.DueForDelivery(1).ToListAsync();

        await Assert.That(claimed.Any(m => m.Id == id)).IsTrue();
    }

    private static async Task<Guid> SeedAsync(IDbContextFactory<OpenShockContext> factory, string recipient, EmailStatus status, DateTime nextAttemptAt)
    {
        await using var db = await factory.CreateDbContextAsync();
        var message = EmailOutboxMessage.Create(
            EmailType.PasswordReset, recipient, "Claim",
            new Dictionary<string, string> { ["k"] = "v" });
        message.Status = status;
        message.NextAttemptAt = nextAttemptAt;
        db.EmailOutbox.Add(message);
        await db.SaveChangesAsync();
        return message.Id;
    }
}
