namespace OpenShock.Common.Models;

/// <summary>
/// Delivery state of an <see cref="OpenShockDb.EmailOutboxMessage"/>.
/// </summary>
/// <remarks>
/// There are deliberately only three states. A separate "pending" state would carry no information
/// here: a message that has been enqueued but not yet delivered is simply one the consumer has not
/// finished sending, which is exactly what <see cref="Sending"/> means. A row waiting for its next
/// retry is also <see cref="Sending"/> (with a future <c>NextAttemptAt</c>), it is still in flight,
/// just not this instant. Collapsing "queued", "in progress" and "awaiting retry" into one state
/// keeps the claim query and the state machine trivial.
/// </remarks>
public enum EmailStatus
{
    /// <summary>
    /// The message still needs to be delivered. Covers freshly enqueued rows, rows currently being
    /// attempted (see <c>AttemptStartedAt</c>), and rows waiting for a retry (see <c>NextAttemptAt</c>).
    /// This is the only state the consumer claims from.
    /// </summary>
    Sending,

    /// <summary>The message was handed to the email provider successfully. Terminal.</summary>
    Sent,

    /// <summary>
    /// The message will not be delivered: it exhausted its retry budget, hit a permanent provider
    /// error, or the underlying request it was for no longer exists. Terminal, but the row is kept
    /// (never auto-deleted) so it stays inspectable and can be requeued by an operator.
    /// </summary>
    Failed
}
