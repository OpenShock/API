using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// A durable, application-owned record of an email the system intends to deliver. This table is the
/// <em>source of truth</em> for "we owe this user this email": a row is written in the same database
/// transaction as the business change that caused it, and a background consumer delivers it,
/// retrying until it succeeds or is exhausted. Rows are never auto-deleted, so failed sends remain
/// visible and can be requeued.
/// </summary>
/// <remarks>
/// <para>
/// <b>Flow.</b> The request handler creates this row (and any related request row, e.g. a
/// <see cref="UserPasswordReset"/>) and commits. It does <em>not</em> send. A single background
/// consumer claims the row, renders the body, mints a fresh secret for token-bearing types, hands
/// the message to the email provider, and records the outcome. Delivery is therefore decoupled from
/// the HTTP request: a provider outage, a process restart, or a crash mid-send cannot lose the
/// email - the row simply stays <see cref="EmailStatus.Sending"/> and is retried.
/// </para>
///
/// <para>
/// <b>Why this lives in our database (rather than a job framework such as Hangfire).</b>
/// This is intentionally an <i>outbox</i>, not a generic background-job queue, and the two solve
/// different problems:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Transactional integrity.</b> The email intent is committed in the <em>same</em> transaction
///     as the change that caused it (account created, reset requested, email changed). There is no
///     window where the business row commits but the email is lost, or where an email is scheduled
///     for a change that then rolls back. A separate job store cannot give this guarantee without,
///     in effect, reinventing an outbox alongside it.
///   </description></item>
///   <item><description>
///     <b>Single source of truth.</b> A job framework's record is "a method was scheduled and ran";
///     this row is the domain fact "this user is owed this email, here is its delivery state". When
///     a user reports a missing email, this table answers it directly (who, which type, how many
///     attempts, last error, sent/failed) - no second system to correlate against.
///   </description></item>
///   <item><description>
///     <b>No new infrastructure or operational surface.</b> Reliability rides on PostgreSQL, which
///     is already the system's durable store. There is no extra schema owned by a third-party
///     scheduler, no dashboard to secure, and no job-serialization/versioning concerns.
///   </description></item>
///   <item><description>
///     <b>Latency.</b> Delivery is push-triggered (a Redis notification on enqueue) with a periodic
///     poll only as a safety net, so first-send is sub-second. A minute-granularity scheduler would
///     not meet that.
///   </description></item>
/// </list>
/// <para>
/// This is not a claim that job frameworks are bad - for recurring/scheduled maintenance work they
/// are the right tool, and the codebase uses one for cron jobs. It is the narrower point that
/// <i>transactional, must-not-be-lost, resendable email</i> is an outbox-shaped problem, and modelling
/// it as a first-class table is the simplest design that satisfies those requirements.
/// </para>
///
/// <para>
/// <b>Why the related auth flows mint their token lazily.</b> Because a working reset/verification
/// link cannot be reconstructed from the stored hash, "store the intent, mint on send" is the only
/// model that keeps the queue free of usable secrets while still allowing a resend. The consumer
/// generates the secret at send time and writes only its hash to the request row (see
/// <see cref="UserPasswordReset.TokenHash"/> etc.; the row is created with a seeded throwaway hash
/// that the consumer overwrites on first send). A resend produces a brand-new token, which is
/// exactly the desired behaviour for security links anyway.
/// </para>
///
/// <para>
/// <b>Delivery guarantee.</b> This is at-least-once. The unavoidable edge case - the provider accepts
/// the message but the process dies before the row is marked <see cref="EmailStatus.Sent"/> - results
/// in one duplicate send on retry. For these email types that is harmless (a second activation /
/// reset / notice), and it is the correct trade against the alternative of silently losing mail.
/// </para>
/// </remarks>
public sealed class EmailOutboxMessage
{
    /// <summary>Primary key (UUIDv7, time-ordered so the queue drains roughly FIFO).</summary>
    public required Guid Id { get; set; }

    /// <summary>Which email this is; selects the template and the send logic.</summary>
    public required EmailType Type { get; set; }

    /// <summary>Destination email address.</summary>
    public required string Recipient { get; set; }

    /// <summary>Optional display name for the recipient.</summary>
    public string? RecipientName { get; set; }

    /// <summary>
    /// Dynamic, non-secret parameters needed to render and send, stored as Postgres <c>jsonb</c>
    /// (Npgsql maps the dictionary to jsonb via dynamic JSON; see <c>EnableDynamicJson</c> in the
    /// context configuration). An open key/value bag keyed by the well-known names in
    /// <see cref="EmailOutboxPayloadKeys"/>, so new email types need no schema change. Never contains
    /// a rendered body or a usable secret.
    /// </summary>
    public required Dictionary<string, string> Payload { get; set; }

    /// <summary>Delivery state. See <see cref="EmailStatus"/>.</summary>
    public EmailStatus Status { get; set; } = EmailStatus.Sending;

    /// <summary>Number of send attempts started so far. Incremented when the row is claimed.</summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Earliest time the next attempt may run. Null means "as soon as possible". Set into the future
    /// after a transient failure to implement back-off.
    /// </summary>
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>
    /// When the current attempt was claimed, acting as a lease. A row whose lease is older than the
    /// lease timeout is considered abandoned (the worker crashed mid-send) and may be re-claimed.
    /// Cleared whenever the row reaches a resting state (sent, failed, or waiting for retry).
    /// </summary>
    public DateTime? AttemptStartedAt { get; set; }

    /// <summary>Last error recorded for a failed/retried attempt. Null while healthy.</summary>
    public string? LastError { get; set; }

    /// <summary>When the row was created (i.e. when the email was enqueued).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the message was successfully handed to the provider. Null until then.</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>When the message reached the terminal <see cref="EmailStatus.Failed"/> state.</summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Builds a new enqueued message in the <see cref="EmailStatus.Sending"/> state. The caller adds
    /// it to the context and commits it together with the related business change.
    /// </summary>
    public static EmailOutboxMessage Create(EmailType type, string recipient, string? recipientName, Dictionary<string, string> payload)
    {
        return new EmailOutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            Recipient = recipient,
            RecipientName = recipientName,
            Payload = payload,
            Status = EmailStatus.Sending
        };
    }
}
