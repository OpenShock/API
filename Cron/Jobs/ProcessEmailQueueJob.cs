using OpenShock.Common.Services.Email.Queue;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Drains the email retry queue: resends emails that previously failed to reach the upstream provider,
/// regenerating a fresh token where needed. Each row respects its own <c>NextAttemptAt</c> backoff, so
/// running every minute simply picks up whatever has become due.
/// </summary>
[CronJob("* * * * *")] // Every minute (https://crontab.guru/)
public sealed class ProcessEmailQueueJob
{
    private readonly EmailQueueProcessor _processor;

    public ProcessEmailQueueJob(EmailQueueProcessor processor)
    {
        _processor = processor;
    }

    public Task Execute(CancellationToken cancellationToken = default)
    {
        return _processor.ProcessDueItemsAsync(cancellationToken);
    }
}
