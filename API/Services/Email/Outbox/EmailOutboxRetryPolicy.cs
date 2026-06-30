using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Services.Email.Outbox;

/// <summary>
/// Pure retry/state-machine logic for the email outbox, factored out of the consumer so it can be
/// unit-tested without a database or a running host.
/// </summary>
public static class EmailOutboxRetryPolicy
{
    /// <summary>Maximum number of attempts before a transiently-failing message is given up as failed.</summary>
    public const int MaxAttempts = 10;

    /// <summary>Upper bound (seconds) of the random jitter added to each retry delay.</summary>
    public const int MaxJitterSeconds = 30;

    /// <summary>
    /// Deterministic part of the back-off: exponential in the attempt count, capped at
    /// <see cref="Duration.EmailOutboxRetryMaxDelay"/>. Exposed (without jitter) for testing.
    /// </summary>
    public static TimeSpan GetBaseDelay(int attemptCount)
    {
        // Compute in seconds (double) and cap before converting, so a large attempt count can't
        // overflow TimeSpan — Math.Pow can reach infinity, which simply compares above the cap.
        var maxDelay = Duration.EmailOutboxRetryMaxDelay;
        var seconds = Duration.EmailOutboxRetryBaseDelay.TotalSeconds * Math.Pow(2, Math.Max(0, attemptCount - 1));
        return seconds >= maxDelay.TotalSeconds ? maxDelay : TimeSpan.FromSeconds(seconds);
    }

    /// <summary>Back-off delay for the next retry: <see cref="GetBaseDelay"/> plus a random jitter.</summary>
    public static TimeSpan GetRetryDelay(int attemptCount)
        => GetBaseDelay(attemptCount) + TimeSpan.FromSeconds(Random.Shared.Next(0, MaxJitterSeconds + 1));

    /// <summary>
    /// Applies a dispatch outcome to a message, advancing it to its next state: delivered, failed
    /// (permanent / skipped / exhausted), or scheduled for another retry. Pure — only mutates
    /// <paramref name="message"/>.
    /// </summary>
    public static void Apply(EmailOutboxMessage message, EmailDispatchResult result, DateTime nowUtc)
    {
        switch (result.Outcome)
        {
            case EmailDispatchOutcome.Sent:
                message.Status = EmailStatus.Sent;
                message.SentAt = nowUtc;
                message.AttemptStartedAt = null;
                message.LastError = null;
                break;

            case EmailDispatchOutcome.Skipped:
                message.Status = EmailStatus.Failed;
                message.FailedAt = nowUtc;
                message.AttemptStartedAt = null;
                message.LastError = Truncate($"Skipped: {result.Detail}");
                break;

            case EmailDispatchOutcome.PermanentFailure:
                message.Status = EmailStatus.Failed;
                message.FailedAt = nowUtc;
                message.AttemptStartedAt = null;
                message.LastError = Truncate(result.Detail);
                break;

            case EmailDispatchOutcome.TransientFailure:
                message.LastError = Truncate(result.Detail);
                message.AttemptStartedAt = null;
                if (message.AttemptCount >= MaxAttempts)
                {
                    message.Status = EmailStatus.Failed;
                    message.FailedAt = nowUtc;
                }
                else
                {
                    message.NextAttemptAt = nowUtc + GetRetryDelay(message.AttemptCount);
                }
                break;
        }
    }

    /// <summary>Clamps an error string to the column's maximum length.</summary>
    public static string? Truncate(string? value)
    {
        if (value is null) return null;
        return value.Length <= HardLimits.EmailOutboxLastErrorMaxLength
            ? value
            : value[..HardLimits.EmailOutboxLastErrorMaxLength];
    }
}
