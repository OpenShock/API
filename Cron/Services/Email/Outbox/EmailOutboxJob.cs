using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Cron.Services.Email.Outbox;

/// <summary>
/// The Hangfire job that delivers a single <see cref="EmailOutboxMessage"/>. One job instance per
/// email, so each message has an independent retry timeline.
/// </summary>
/// <remarks>
/// Hangfire owns the delivery machinery: <see cref="AutomaticRetryAttribute"/> supplies the back-off
/// curve, the persistent retry schedule, and crash requeue; the job only maps a dispatch outcome onto
/// the row's terminal state. A transient failure is surfaced by <em>throwing</em>, which is what makes
/// Hangfire schedule the next retry. On the final attempt (detected via the <c>RetryCount</c> job
/// parameter) the row is marked <see cref="EmailStatus.Failed"/> before the throw, so an exhausted
/// message never stays stuck looking like it is still in flight.
/// </remarks>
public sealed class EmailOutboxJob
{
    /// <summary>Number of automatic retries before a transiently-failing message is given up as failed.</summary>
    public const int MaxRetries = 10;

    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly IEmailOutboxDispatcher _dispatcher;
    private readonly ILogger<EmailOutboxJob> _logger;

    /// <summary>DI constructor.</summary>
    public EmailOutboxJob(IDbContextFactory<OpenShockContext> dbContextFactory, IEmailOutboxDispatcher dispatcher, ILogger<EmailOutboxJob> logger)
    {
        _dbContextFactory = dbContextFactory;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>Delivers the message with the given id. See the type remarks for the retry contract.</summary>
    [AutomaticRetry(Attempts = MaxRetries, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task SendAsync(Guid messageId, PerformContext? context, CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var message = await db.EmailOutbox.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message is null)
        {
            _logger.LogWarning("Email outbox message {MessageId} no longer exists; nothing to send", messageId);
            return;
        }

        // Idempotency: a duplicate enqueue or a crash-requeue may re-run a message already resolved.
        if (message.Status is EmailStatus.Sent or EmailStatus.Failed) return;

        var result = await _dispatcher.SendAsync(message, db, cancellationToken);
        var nowUtc = DateTime.UtcNow;

        switch (result.Outcome)
        {
            case EmailDispatchOutcome.Sent:
                message.Status = EmailStatus.Sent;
                message.SentAt = nowUtc;
                message.LastError = null;
                await db.SaveChangesAsync(cancellationToken);
                return;

            case EmailDispatchOutcome.Skipped:
                message.Status = EmailStatus.Failed;
                message.FailedAt = nowUtc;
                message.LastError = Truncate($"Skipped: {result.Detail}");
                await db.SaveChangesAsync(cancellationToken);
                return;

            case EmailDispatchOutcome.PermanentFailure:
                message.Status = EmailStatus.Failed;
                message.FailedAt = nowUtc;
                message.LastError = Truncate(result.Detail);
                await db.SaveChangesAsync(cancellationToken);
                return;

            case EmailDispatchOutcome.TransientFailure:
                message.LastError = Truncate(result.Detail);

                // RetryCount is the number of retries already performed (0 on the first run). When it has
                // reached the cap this is the last attempt, so record the terminal failure on the row;
                // the throw below then lets Hangfire move the job to its Failed state too.
                var retryCount = context?.GetJobParameter<int>("RetryCount") ?? 0;
                if (retryCount >= MaxRetries)
                {
                    message.Status = EmailStatus.Failed;
                    message.FailedAt = nowUtc;
                    await db.SaveChangesAsync(cancellationToken);
                    _logger.LogWarning("Email outbox message {MessageId} ({Type}) failed after {Attempts} attempt(s): {Detail}",
                        message.Id, message.Type, retryCount + 1, result.Detail);
                }
                else
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

                throw new EmailOutboxTransientException(result.Detail ?? "Transient email send failure");

            default:
                throw new InvalidOperationException($"Unhandled dispatch outcome {result.Outcome}");
        }
    }

    /// <summary>Clamps an error string to the column's maximum length.</summary>
    private static string? Truncate(string? value)
        => value is null || value.Length <= HardLimits.EmailOutboxLastErrorMaxLength
            ? value
            : value[..HardLimits.EmailOutboxLastErrorMaxLength];
}

/// <summary>Thrown to signal a transient send failure so Hangfire schedules a retry on its back-off curve.</summary>
public sealed class EmailOutboxTransientException : Exception
{
    /// <summary>Creates the exception with the underlying transient failure detail.</summary>
    public EmailOutboxTransientException(string message) : base(message) { }
}
