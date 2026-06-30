using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.RedisPubSub;
using StackExchange.Redis;

namespace OpenShock.API.Services.Email.Outbox;

/// <summary>
/// The email outbox consumer: the single component that delivers enqueued
/// <see cref="EmailOutboxMessage"/> rows, retrying until they succeed or are exhausted.
/// </summary>
/// <remarks>
/// <para>
/// <b>Push first, poll as a safety net.</b> Delivery is triggered by a Redis notification published
/// the instant a message is enqueued (<see cref="RedisChannels.EmailOutboxPending"/>), so first-send
/// latency is sub-second rather than tied to a polling interval. A periodic tick
/// (<see cref="Duration.EmailOutboxPollInterval"/>) still runs as a fallback: it picks up due retries
/// (whose <c>NextAttemptAt</c> is in the future), re-claims leases abandoned by a crashed instance,
/// and covers the rare case of a dropped notification. The notification can therefore be best-effort
/// - losing one only delays delivery to the next tick, it can never lose the email.
/// </para>
/// <para>
/// <b>Runs in every API instance, safely.</b> Rows are claimed with <c>FOR UPDATE SKIP LOCKED</c>, so
/// when several instances react to the same notification each grabs a disjoint set of rows and they
/// drain in parallel without ever sending the same message twice in a batch. Claiming also stamps a
/// lease (<c>AttemptStartedAt</c>); a row whose lease is older than
/// <see cref="Duration.EmailOutboxLeaseTimeout"/> is considered abandoned and re-claimed, which is how
/// a send interrupted by a crash recovers.
/// </para>
/// <para>
/// This lives in the API host rather than the cron/Hangfire host on purpose: it needs
/// <see cref="IEmailService"/> (which the API host owns) and sub-second latency, which a
/// minute-granularity scheduler cannot provide. Periodic <em>maintenance</em> of this table (pruning
/// old delivered rows) is the appropriate cron job, and is separate from sending.
/// </para>
/// </remarks>
public sealed class EmailOutboxWorker : BackgroundService
{
    private const int BatchSize = 20;

    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly IEmailOutboxDispatcher _dispatcher;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<EmailOutboxWorker> _logger;

    // Coalescing wake signal: capacity 1, so any number of notifications between drains collapse into
    // a single "there is work" wake-up.
    private readonly SemaphoreSlim _wakeSignal = new(0, 1);

    /// <summary>DI constructor.</summary>
    public EmailOutboxWorker(
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IEmailOutboxDispatcher dispatcher,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<EmailOutboxWorker> logger)
    {
        _dbContextFactory = dbContextFactory;
        _dispatcher = dispatcher;
        _subscriber = connectionMultiplexer.GetSubscriber();
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _subscriber.SubscribeAsync(RedisChannels.EmailOutboxPending, OnPendingNotification);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DrainAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email outbox drain failed");
                }

                // Wait for a push notification or the fallback poll interval, whichever comes first.
                try
                {
                    await _wakeSignal.WaitAsync(Duration.EmailOutboxPollInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            await _subscriber.UnsubscribeAsync(RedisChannels.EmailOutboxPending, OnPendingNotification);
        }
    }

    private void OnPendingNotification(RedisChannel channel, RedisValue value)
    {
        // Release the coalescing signal; ignore if already signalled.
        try
        {
            _wakeSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already pending a wake-up - nothing to do.
        }
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        int processed;
        do
        {
            processed = await ProcessBatchAsync(cancellationToken);
        }
        while (processed >= BatchSize && !cancellationToken.IsCancellationRequested);
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var leaseCutoff = nowUtc - Duration.EmailOutboxLeaseTimeout;

        // Atomically claim a batch of due rows. FOR UPDATE SKIP LOCKED lets concurrent instances take
        // disjoint sets; stamping the lease + bumping the attempt count is what reserves them.
        List<EmailOutboxMessage> claimed;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            claimed = await db.EmailOutbox.FromSql(
                $"""
                 SELECT * FROM email_outbox
                 WHERE status = {EmailStatus.Sending}
                   AND (next_attempt_at IS NULL OR next_attempt_at <= {nowUtc})
                   AND (attempt_started_at IS NULL OR attempt_started_at < {leaseCutoff})
                 ORDER BY created_at
                 LIMIT {BatchSize}
                 FOR UPDATE SKIP LOCKED
                 """).ToListAsync(cancellationToken);

            if (claimed.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return 0;
            }

            foreach (var message in claimed)
            {
                message.AttemptStartedAt = nowUtc;
                message.AttemptCount++;
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // Send outside the claim transaction so a slow provider never holds row locks. Each message is
        // marked durably right after its attempt, so a crash mid-batch only re-sends the ones whose
        // lease has not yet been resolved.
        foreach (var message in claimed)
        {
            var result = await _dispatcher.SendAsync(message, db, cancellationToken);
            EmailOutboxRetryPolicy.Apply(message, result, DateTime.UtcNow);
            await db.SaveChangesAsync(cancellationToken);

            if (message.Status == EmailStatus.Failed)
                _logger.LogWarning("Email outbox message {MessageId} ({Type}) failed after {Attempts} attempt(s): {Detail}",
                    message.Id, message.Type, message.AttemptCount, result.Detail);
        }

        return claimed.Count;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _wakeSignal.Dispose();
        base.Dispose();
    }
}
