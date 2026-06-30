using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;
using OpenShock.Cron.Services.Email.Outbox;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Claims due rows from the email_outbox table and delivers them, recording attempt count, retry time,
/// and terminal state back on each row. The table is the only place delivery state lives - Hangfire
/// just schedules this job. Runs on the recurring schedule below (retry sweep + safety net) and is also
/// enqueued on demand by <see cref="EmailOutboxNotificationListener"/> the instant a row is written, so
/// fresh mail goes out without waiting for the next minute.
/// </summary>
/// <remarks>
/// Concurrent runs are safe: a row is claimed with FOR UPDATE SKIP LOCKED and leased (Sending) with an
/// incremented attempt_count, which acts as the lease's fencing token. The terminal write is guarded by
/// it (mapped as a concurrency token), so if a lapsed lease lets another run reclaim a row mid-batch
/// (bumping attempt_count) the loser's write fails with <see cref="DbUpdateConcurrencyException"/>
/// instead of clobbering it. Each message is delivered in isolation (its own reload + change-tracker
/// reset and its own try/catch), so one failure never aborts the rest of the batch, and the whole
/// backlog is drained batch by batch rather than 50 per run.
/// </remarks>
[CronJob("* * * * *")] // Every minute (https://crontab.guru/)
public sealed class EmailOutboxDeliveryJob
{
    private const int BatchSize = 50;

    private readonly OpenShockContext _db;
    private readonly IEmailOutboxDispatcher _dispatcher;
    private readonly ILogger<EmailOutboxDeliveryJob> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="dispatcher"></param>
    /// <param name="logger"></param>
    public EmailOutboxDeliveryJob(OpenShockContext db, IEmailOutboxDispatcher dispatcher, ILogger<EmailOutboxDeliveryJob> logger)
    {
        _db = db;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Execute()
    {
        int claimed;
        do
        {
            var ids = await ClaimDueBatchAsync();
            claimed = ids.Count;

            foreach (var id in ids)
            {
                try
                {
                    await DeliverAsync(id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // A lapsed lease let another run reclaim this row mid-batch; it now owns delivery.
                    _logger.LogDebug("Email outbox message {MessageId} was reclaimed concurrently; skipping", id);
                }
                catch (Exception ex)
                {
                    // Isolate the failure to this message: the row stays Sending and is retried when its
                    // lease lapses, while the rest of the batch still goes out.
                    _logger.LogError(ex, "Unexpected error delivering email outbox message {MessageId}", id);
                }
            }
        }
        while (claimed >= BatchSize);
    }

    /// <summary>
    /// Atomically claims up to <see cref="BatchSize"/> due rows - <see cref="EmailStatus.Pending"/> rows
    /// past their next-attempt time, or <see cref="EmailStatus.Sending"/> rows whose lease has lapsed (a
    /// crashed prior run) - flipping each to <see cref="EmailStatus.Sending"/> with a fresh lease and an
    /// incremented attempt count. A row that has run out of attempts is failed in place instead. The lock
    /// is held only for this short transaction; the sends happen afterwards, outside it. Returns the ids
    /// now owned by this run.
    /// </summary>
    private async Task<List<Guid>> ClaimDueBatchAsync()
    {
        _db.ChangeTracker.Clear();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var due = await _db.EmailOutbox.FromSql(
            $"""
             SELECT * FROM email_outbox
             WHERE next_attempt_at <= now()
               AND (status = {EmailStatus.Pending} OR status = {EmailStatus.Sending})
             ORDER BY next_attempt_at
             LIMIT {BatchSize}
             FOR UPDATE SKIP LOCKED
             """).ToListAsync();

        if (due.Count == 0)
        {
            await transaction.CommitAsync();
            return [];
        }

        var nowUtc = DateTime.UtcNow;
        var claimed = new List<Guid>(due.Count);

        foreach (var message in due)
        {
            if (EmailOutboxStateMachine.TryClaim(message, nowUtc))
            {
                claimed.Add(message.Id);
            }
            else
            {
                _logger.LogWarning("Email outbox message {MessageId} ({Type}) exhausted via lease reclaim after {Attempts} attempt(s)",
                    message.Id, message.Type, message.AttemptCount - 1);
            }
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return claimed;
    }

    /// <summary>
    /// Delivers a single claimed message and records the outcome on its row. Reloads the row fresh (its
    /// own change-tracker reset) so a failure on the previous message can't carry over and so the write
    /// carries the current concurrency token; if the row is no longer <see cref="EmailStatus.Sending"/>
    /// another run already resolved it and this one bows out.
    /// </summary>
    private async Task DeliverAsync(Guid messageId)
    {
        _db.ChangeTracker.Clear();

        var message = await _db.EmailOutbox.FirstOrDefaultAsync(m => m.Id == messageId);
        if (message is null)
        {
            _logger.LogWarning("Email outbox message {MessageId} no longer exists; nothing to send", messageId);
            return;
        }

        if (message.Status != EmailStatus.Sending) return;

        // Newest-wins coalescing: if a strictly-newer row shares this row's coalesce key, this row is a
        // stale duplicate (a superseded request, or a redrive of an old dead row after a newer one was
        // already sent) - skip it instead of sending. The queue only compares the opaque key; what it
        // means (and whether to set one at all) is the enqueueing domain's choice. A null key never
        // coalesces. Rows are time-ordered by their UUIDv7 id, so "newest" is just the max id in the group.
        if (message.CoalesceKey is not null)
        {
            var newestId = await _db.EmailOutbox
                .Where(m => m.CoalesceKey == message.CoalesceKey)
                .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
                .Select(m => m.Id)
                .FirstAsync();

            if (newestId != message.Id)
            {
                EmailOutboxStateMachine.ApplyResult(message, EmailDispatchResult.Skip("Superseded by a newer request"), DateTime.UtcNow);
                await _db.SaveChangesAsync();
                return;
            }
        }

        var result = await _dispatcher.SendAsync(message, _db);

        if (result.Outcome == EmailDispatchOutcome.TransientFailure && message.AttemptCount >= EmailOutboxRetryPolicy.MaxAttempts)
        {
            _logger.LogWarning("Email outbox message {MessageId} ({Type}) failed after {Attempts} attempt(s): {Detail}",
                message.Id, message.Type, message.AttemptCount, result.Detail);
        }

        EmailOutboxStateMachine.ApplyResult(message, result, DateTime.UtcNow);
        await _db.SaveChangesAsync();
    }
}
