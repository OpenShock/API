namespace OpenShock.Common.Models;

/// <summary>
/// Delivery state of an <see cref="OpenShockDb.EmailOutboxMessage"/>.
/// </summary>
/// <remarks>
/// The row is written by the API as <see cref="Pending"/> in the same transaction as the business
/// change. The Cron consumer claims pending rows and hands each to Hangfire for delivery, flipping it
/// to <see cref="Queued"/>; from there Hangfire owns execution, retry scheduling, and crash recovery
/// until the message reaches a terminal <see cref="Sent"/> or <see cref="Failed"/> state.
/// </remarks>
public enum EmailStatus
{
    /// <summary>
    /// Freshly enqueued by the API and not yet handed to the sender. This is the only state the Cron
    /// consumer claims from.
    /// </summary>
    Pending,

    /// <summary>
    /// Handed off to Hangfire for delivery: a send job is enqueued, running, or scheduled for a
    /// retry. The consumer never re-claims a row in this state - Hangfire drives it to a terminal
    /// state.
    /// </summary>
    Queued,

    /// <summary>The message was handed to the email provider successfully. Terminal.</summary>
    Sent,

    /// <summary>
    /// The message will not be delivered: it exhausted its retry budget, hit a permanent provider
    /// error, or the underlying request it was for no longer exists. Terminal, but the row is kept
    /// (never auto-deleted) so it stays inspectable and can be requeued by an operator.
    /// </summary>
    Failed
}
