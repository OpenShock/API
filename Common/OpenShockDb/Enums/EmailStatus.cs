using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Delivery state of an <see cref="EmailOutboxMessage"/>.
/// </summary>
/// <remarks>
/// The row is written by the API as <see cref="Pending"/> in the same transaction as the business
/// change. The Cron delivery job is the sole executor: it claims a due row (flipping it to
/// <see cref="Sending"/> under a lease), hands it to the email provider, and records the outcome
/// directly on the row - a terminal <see cref="Sent"/>, <see cref="Failed"/>, or <see cref="Skipped"/>,
/// or back to <see cref="Pending"/> with a future retry time. Retry count and scheduling live in the
/// row itself (<see cref="EmailOutboxMessage.AttemptCount"/> /
/// <see cref="EmailOutboxMessage.NextAttemptAt"/>), so the table is the complete, queryable
/// source of truth for every email's delivery state.
/// </remarks>
[PgEnum(Name = "email_status")]
public enum EmailStatus
{
    /// <summary>
    /// Due for delivery: either freshly enqueued, or a transient failure scheduled for a later retry
    /// (distinguished by <see cref="EmailOutboxMessage.NextAttemptAt"/> being in the
    /// future and <see cref="EmailOutboxMessage.AttemptCount"/> &gt; 0). A row is eligible
    /// to be claimed once its next-attempt time has passed.
    /// </summary>
    [PgName("pending")] Pending = 0,

    /// <summary>
    /// Claimed by an executor and currently being delivered. The claim carries a lease
    /// (<see cref="EmailOutboxMessage.NextAttemptAt"/> set to the lease expiry); if the
    /// process dies mid-send, the lease lapses and the row is reclaimed - so a crash cannot strand a
    /// message in this state.
    /// </summary>
    [PgName("sending")] Sending = 1,

    /// <summary>The message was handed to the email provider successfully. Terminal.</summary>
    [PgName("sent")] Sent = 2,

    /// <summary>
    /// Delivery was abandoned: it exhausted its retry budget or hit a permanent provider error. The
    /// row is kept (never auto-deleted) so it stays inspectable and can be requeued by an operator.
    /// Terminal.
    /// </summary>
    [PgName("failed")] Failed = 3,

    /// <summary>
    /// The email was intentionally not sent because the underlying request no longer needs it (the
    /// request was used, expired, superseded by a newer credential change, or no longer exists). This
    /// is a successful no-op, kept distinct from <see cref="Failed"/> so operators never mistake it for
    /// a delivery problem or requeue it. Terminal.
    /// </summary>
    [PgName("skipped")] Skipped = 4
}
