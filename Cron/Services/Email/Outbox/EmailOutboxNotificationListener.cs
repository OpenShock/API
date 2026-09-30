using Hangfire;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Cron.Jobs;
using StackExchange.Redis;

namespace OpenShock.Cron.Services.Email.Outbox;

/// <summary>
/// Bridges the Redis "email enqueued" notification to Hangfire. When the API publishes
/// <see cref="RedisChannels.EmailOutboxPending"/> after writing an outbox row, this enqueues an
/// immediate <see cref="EmailOutboxDeliveryJob"/> so fresh mail is delivered within Hangfire's pickup
/// latency instead of waiting for the next scheduled (every-minute) sweep.
/// </summary>
/// <remarks>
/// It is event-driven - a Redis subscription, not a polling loop - and holds no delivery state: the
/// notification is a payload-less nudge, and the job reads the actual due rows from the table. A
/// dropped notification or a failed enqueue only delays delivery to the next scheduled sweep, so the
/// callback never throws.
/// </remarks>
public sealed class EmailOutboxNotificationListener : IHostedService
{
    private readonly ISubscriber _subscriber;
    private readonly IBackgroundJobClient _jobClient;
    private readonly ILogger<EmailOutboxNotificationListener> _logger;

    /// <summary>DI constructor.</summary>
    public EmailOutboxNotificationListener(
        IConnectionMultiplexer connectionMultiplexer,
        IBackgroundJobClient jobClient,
        ILogger<EmailOutboxNotificationListener> logger)
    {
        _subscriber = connectionMultiplexer.GetSubscriber();
        _jobClient = jobClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) =>
        _subscriber.SubscribeAsync(RedisChannels.EmailOutboxPending, OnPendingNotification);

    private void OnPendingNotification(RedisChannel channel, RedisValue value)
    {
        try
        {
            _jobClient.Enqueue<EmailOutboxDeliveryJob>(job => job.Execute());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue email outbox delivery from pending notification; delivery falls back to the scheduled sweep");
        }
    }

    /// <inheritdoc />
    public  Task StopAsync(CancellationToken cancellationToken) =>
        _subscriber.UnsubscribeAsync(RedisChannels.EmailOutboxPending, OnPendingNotification);
}
