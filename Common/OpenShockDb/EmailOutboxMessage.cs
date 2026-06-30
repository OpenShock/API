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
/// <b>Flow.</b> The API request handler creates this row (and any related request row, e.g. a
/// <see cref="UserPasswordReset"/>) and commits it as <see cref="EmailStatus.Pending"/>. It does
/// <em>not</em> send. The Cron host's outbox consumer claims pending rows and hands each to Hangfire,
/// which runs the send job: it renders the body, mints a fresh secret for token-bearing types, hands
/// the message to the email provider, and records the outcome. Delivery is therefore decoupled from
/// the HTTP request: a provider outage, a process restart, or a crash mid-send cannot lose the email.
/// </para>
///
/// <para>
/// <b>Why this row exists alongside Hangfire (it is an outbox, not a duplicate job queue).</b>
/// The two layers own different things and are deliberately combined:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>This row owns transactional integrity + the audit fact.</b> The email intent is committed
///     in the <em>same</em> transaction as the change that caused it (account created, reset
///     requested, email changed), so there is no window where the business row commits but the email
///     is lost, nor an email scheduled for a change that then rolls back. The row is also the domain
///     answer to "is this user owed this email, and what is its delivery state" (who, which type,
///     how many attempts, last error, sent/failed) - a single place to look, kept forever.
///   </description></item>
///   <item><description>
///     <b>Hangfire owns the delivery machinery.</b> Once the consumer hands a row off, Hangfire owns
///     durable execution, retry scheduling, crash requeue, and the operator dashboard - the parts a
///     bespoke worker would otherwise have to reinvent. The back-off curve itself is supplied by
///     <c>EmailOutboxRetryPolicy</c>, which the send job feeds into Hangfire's scheduler.
///   </description></item>
/// </list>
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

    /// <summary>Delivery state. See <see cref="EmailStatus"/>. Retry count and scheduling are owned by
    /// Hangfire once the row is handed off, so they are not tracked here.</summary>
    public EmailStatus Status { get; set; } = EmailStatus.Pending;

    /// <summary>Last error recorded for a failed/retried attempt. Null while healthy.</summary>
    public string? LastError { get; set; }

    /// <summary>When the row was created (i.e. when the email was enqueued).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the message was successfully handed to the provider. Null until then.</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>When the message reached the terminal <see cref="EmailStatus.Failed"/> state.</summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Builds a new enqueued message in the <see cref="EmailStatus.Pending"/> state. The caller adds
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
            Status = EmailStatus.Pending
        };
    }
}
