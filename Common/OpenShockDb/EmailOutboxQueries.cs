using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Reusable queries over the <see cref="EmailOutboxMessage"/> table. Kept here (rather than inline in the
/// Cron delivery job) so the raw SQL is a single source of truth that integration tests can execute
/// directly - a table or column rename then breaks the test rather than surfacing only in production.
/// </summary>
public static class EmailOutboxQueries
{
    /// <summary>How many due rows the delivery job claims per batch.</summary>
    public const int ClaimBatchSize = 50;

    /// <summary>
    /// The delivery job's claim query: due rows - <see cref="EmailStatus.Pending"/> past their
    /// next-attempt time, or <see cref="EmailStatus.Sending"/> whose lease has lapsed - oldest first,
    /// capped at <paramref name="batchSize"/>, locked with <c>FOR UPDATE SKIP LOCKED</c> so concurrent
    /// runs take disjoint batches. Run it inside an explicit transaction to hold the locks for the claim.
    /// </summary>
    public static IQueryable<EmailOutboxMessage> DueForDelivery(this DbSet<EmailOutboxMessage> outbox, int batchSize) =>
        outbox.FromSql(
            $"""
             SELECT * FROM email_outbox
             WHERE next_attempt_at <= now()
               AND (status = {EmailStatus.Pending} OR status = {EmailStatus.Sending})
             ORDER BY next_attempt_at
             LIMIT {batchSize}
             FOR UPDATE SKIP LOCKED
             """);
}
