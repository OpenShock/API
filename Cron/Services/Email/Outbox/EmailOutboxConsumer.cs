using Hangfire;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.RedisPubSub;
using StackExchange.Redis;

namespace OpenShock.Cron.Services.Email.Outbox;

/// <summary>
/// The email-outbox hand-off: claims freshly enqueued (<see cref="EmailStatus.Pending"/>) rows and
/// hands each to Hangfire as an <see cref="EmailOutboxJob"/>. It does <em>not</em> send or retry -
/// once a row is enqueued, Hangfire owns its delivery, retry scheduling, and crash recovery.
/// </summary>
/// <remarks>
/// <para>
/// <b>Push first, sweep as a safety net.</b> Hand-off is triggered by a Redis notification published
/// the instant a message is enqueued (<see cref="RedisChannels.EmailOutboxPending"/>), so the job is
/// queued within sub-seconds. A periodic sweep (<see cref="Duration.EmailOutboxPollInterval"/>) also
/// runs as a fallback to cover a dropped notification. Because both feed a single coalescing wake
/// signal, the push and the sweep can never fire "on top of each other": any number of signals
/// between drains collapse into one drain, so a notification that lands just before a sweep simply
/// shares the same pass.
/// </para>
/// <para>
/// <b>Claims are safe across instances.</b> Rows are claimed with <c>FOR UPDATE SKIP LOCKED</c>, so if
/// the Cron host is scaled out each instance grabs a disjoint set. Hangfire jobs are enqueued before
/// the claim transaction commits: if the process dies in between, the transaction rolls back and the
/// rows stay <see cref="EmailStatus.Pending"/> to be re-handed-off next pass - at worst a duplicate
/// job, which the idempotent <see cref="EmailOutboxJob"/> absorbs.
/// </para>
/// </remarks>
public sealed class EmailOutboxConsumer : BackgroundService
{
    private const int BatchSize = 50;

    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly IBackgroundJobClient _jobClient;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<EmailOutboxConsumer> _logger;

    // Coalescing wake signal: capacity 1, so any number of notifications between drains collapse into
    // a single "there is work" wake-up.
    private readonly SemaphoreSlim _wakeSignal = new(0, 1);

    /// <summary>DI constructor.</summary>
    public EmailOutboxConsumer(
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IBackgroundJobClient jobClient,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<EmailOutboxConsumer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _jobClient = jobClient;
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
                    _logger.LogError(ex, "Email outbox hand-off failed");
                }

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
        int handed;
        do
        {
            handed = await HandOffBatchAsync(cancellationToken);
        }
        while (handed >= BatchSize && !cancellationToken.IsCancellationRequested);
    }

    private async Task<int> HandOffBatchAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var claimed = await db.EmailOutbox.FromSql(
            $"""
             SELECT * FROM email_outbox
             WHERE status = {EmailStatus.Pending}
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
            // Enqueue first, flip to Queued second: if we crash here the rollback leaves the row Pending
            // (re-handed-off next pass), never Queued-with-no-job.
            _jobClient.Enqueue<EmailOutboxJob>(job => job.SendAsync(message.Id, null, CancellationToken.None));
            message.Status = EmailStatus.Queued;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return claimed.Count;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _wakeSignal.Dispose();
        base.Dispose();
    }
}
