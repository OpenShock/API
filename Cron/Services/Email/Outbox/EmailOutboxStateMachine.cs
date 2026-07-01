using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.Cron.Services.Email.Outbox;

/// <summary>
/// The pure state transitions of an <see cref="EmailOutboxMessage"/>: claiming a due row for delivery
/// and recording a delivery outcome. Kept free of I/O so the delivery contract (including the retry
/// back-off and the exhaustion cut-off) is unit-testable in isolation; the delivery job is the only
/// caller and owns the surrounding database transaction.
/// </summary>
public static class EmailOutboxStateMachine
{
    /// <summary>
    /// Claims <paramref name="message"/> for a delivery attempt: counts the attempt and, unless the
    /// attempt budget is now exhausted, marks it <see cref="EmailStatus.Sending"/> with a fresh lease.
    /// Returns <c>true</c> if the caller should now deliver it, or <c>false</c> if it was failed in
    /// place because it ran out of attempts (only reachable via repeated lease-expiry reclaims of a
    /// send that keeps crashing).
    /// </summary>
    public static bool TryClaim(EmailOutboxMessage message, DateTime nowUtc)
    {
        message.AttemptCount++;

        if (message.AttemptCount > EmailOutboxRetryPolicy.MaxAttempts)
        {
            message.Status = EmailStatus.Failed;
            message.FailedAt = nowUtc;
            message.LastError = $"Exhausted after {message.AttemptCount - 1} attempt(s) (delivery did not complete)".Truncate(HardLimits.EmailOutboxLastErrorMaxLength);
            return false;
        }

        message.Status = EmailStatus.Sending;
        message.NextAttemptAt = nowUtc + EmailOutboxRetryPolicy.DeliveryLease;
        return true;
    }

    /// <summary>
    /// Records the result of a delivery attempt on <paramref name="message"/>: a terminal
    /// <see cref="EmailStatus.Sent"/>/<see cref="EmailStatus.Failed"/>/<see cref="EmailStatus.Skipped"/>,
    /// or - for a transient failure with attempts to spare - back to <see cref="EmailStatus.Pending"/>
    /// with the next-attempt time pushed out on the back-off curve. Assumes <see cref="TryClaim"/> has
    /// already counted this attempt.
    /// </summary>
    public static void ApplyResult(EmailOutboxMessage message, EmailDispatchResult result, DateTime nowUtc)
    {
        switch (result.Outcome)
        {
            case EmailDispatchOutcome.Sent:
                message.Status = EmailStatus.Sent;
                message.SentAt = nowUtc;
                message.LastError = null;
                break;

            case EmailDispatchOutcome.Skipped:
                // A successful no-op (request used/expired/superseded/gone). Distinct from Failed so it
                // is never mistaken for a delivery problem or requeued.
                message.Status = EmailStatus.Skipped;
                message.LastError = result.Detail?.Truncate(HardLimits.EmailOutboxLastErrorMaxLength);
                break;

            case EmailDispatchOutcome.PermanentFailure:
                message.Status = EmailStatus.Failed;
                message.FailedAt = nowUtc;
                message.LastError = result.Detail?.Truncate(HardLimits.EmailOutboxLastErrorMaxLength);
                break;

            case EmailDispatchOutcome.TransientFailure:
                message.LastError = result.Detail?.Truncate(HardLimits.EmailOutboxLastErrorMaxLength);
                if (message.AttemptCount >= EmailOutboxRetryPolicy.MaxAttempts)
                {
                    message.Status = EmailStatus.Failed;
                    message.FailedAt = nowUtc;
                }
                else
                {
                    message.Status = EmailStatus.Pending;
                    message.NextAttemptAt = nowUtc + EmailOutboxRetryPolicy.BackoffFor(message.AttemptCount);
                }
                break;

            default:
                throw new InvalidOperationException($"Unhandled dispatch outcome {result.Outcome}");
        }
    }
}
