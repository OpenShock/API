namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// A durable, application-owned record of an email the system intends to deliver. This table is the
/// <em>source of truth</em> for "we owe this user this email" <em>and</em> for its delivery progress:
/// a row is written in the same database transaction as the business change that caused it, and the
/// Cron delivery job delivers it, retrying on its own schedule until it succeeds or is exhausted. Rows are
/// never auto-deleted, so failed sends remain visible and can be requeued.
/// </summary>
/// <remarks>
/// <para>
/// <b>Flow.</b> The API request handler creates this row (and any related request row, e.g. a
/// <see cref="UserPasswordReset"/>) and commits it as <see cref="EmailStatus.Pending"/>. It does
/// <em>not</em> send. The Cron host's outbox delivery job claims due rows, marks each
/// <see cref="EmailStatus.Sending"/> under a lease, renders the body, mints a fresh secret for
/// token-bearing types, hands the message to the email provider, and records the outcome on this row.
/// Delivery is therefore decoupled from the HTTP request: a provider outage, a process restart, or a
/// crash mid-send cannot lose the email.
/// </para>
///
/// <para>
/// <b>This row owns the whole lifecycle - there is no second queue.</b> Unlike the typical
/// outbox-in-front-of-a-job-queue pattern, delivery state is not handed off to an external scheduler.
/// Attempt count and the next-retry time live here (<see cref="AttemptCount"/> /
/// <see cref="NextAttemptAt"/>), the back-off curve is supplied by <c>EmailOutboxRetryPolicy</c>, and
/// the delivery job claims work straight from this table with <c>FOR UPDATE SKIP LOCKED</c>. So a single
/// SQL query answers "is this user owed this email, what is its delivery state, how many attempts, when
/// is the next one, and what was the last error" - all in a table we own and can query, with nothing
/// mirrored into a third-party store.
/// </para>
///
/// <para>
/// <b>Why the related auth flows mint their token lazily, and resends invalidate older links.</b>
/// A working reset/verification link cannot be reconstructed from the stored hash, so "store the
/// intent, mint on send" is the only model that keeps the queue free of usable secrets while still
/// allowing a resend. The delivery job generates the secret at send time and writes only its hash to the
/// request row (see <see cref="UserPasswordReset.TokenHash"/> etc.; the row is created with a seeded
/// throwaway hash that the delivery job overwrites on first send). Because each send overwrites that hash,
/// <b>every newly sent link invalidates all previously sent ones</b> - only the most recent email's
/// link is ever valid, which is the desired behaviour for security links.
/// </para>
///
/// <para>
/// <b>Delivery guarantee.</b> This is at-least-once. The unavoidable edge case - the provider accepts
/// the message but the process dies before the row is marked <see cref="EmailStatus.Sent"/> - results
/// in one duplicate send on retry (carrying a fresh link that supersedes the first, per above). For
/// these email types that is harmless, and it is the correct trade against the alternative of silently
/// losing mail.
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

    /// <summary>
    /// Optional opaque coalescing key. When set, only the newest outbox row sharing this key is
    /// actually delivered; older siblings are skipped as superseded (see the delivery job). The key is
    /// stamped by the domain that enqueues the message (e.g. <c>pwreset:{userId}</c>) and is treated as
    /// an opaque string by the queue - the delivery path never parses it. A <c>null</c> key means
    /// "always deliver, never coalesce" (every row stands on its own).
    /// </summary>
    public string? CoalesceKey { get; set; }

    /// <summary>Delivery state. See <see cref="EmailStatus"/>.</summary>
    public EmailStatus Status { get; set; } = EmailStatus.Pending;

    /// <summary>
    /// How many delivery attempts have been started for this message. Incremented when the delivery job
    /// claims the row, so a crash that strands the row in <see cref="EmailStatus.Sending"/> still
    /// counts as an attempt and the message cannot be retried forever.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// When the row is next eligible for delivery. Set to the creation time on enqueue (so it is due
    /// immediately), pushed into the future by the retry back-off after a transient failure, and used
    /// as the lease expiry while <see cref="EmailStatus.Sending"/> so a crashed attempt is reclaimed.
    /// </summary>
    public DateTime NextAttemptAt { get; set; }

    /// <summary>Last error recorded for a failed/retried/skipped attempt. Null while healthy.</summary>
    public string? LastError { get; set; }

    /// <summary>When the row was created (i.e. when the email was enqueued).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the message was successfully handed to the provider. Null until then.</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>When the message reached the terminal <see cref="EmailStatus.Failed"/> state.</summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Builds a new enqueued message in the <see cref="EmailStatus.Pending"/> state, due immediately.
    /// The caller adds it to the context and commits it together with the related business change.
    /// <paramref name="coalesceKey"/> is the optional opaque coalescing key (see <see cref="CoalesceKey"/>):
    /// a stable per-intent key (e.g. <c>pwreset:{userId}</c>) makes only the newest such request deliver;
    /// <c>null</c> means always deliver. Prefer the per-type factories (<see cref="ForPasswordReset"/> etc.).
    /// </summary>
    public static EmailOutboxMessage Create(EmailType type, string recipient, string? recipientName, Dictionary<string, string> payload, string? coalesceKey = null)
    {
        return new EmailOutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            Recipient = recipient,
            RecipientName = recipientName,
            Payload = payload,
            CoalesceKey = coalesceKey,
            Status = EmailStatus.Pending
        };
    }

    // Per-type builders: each one owns the (type, payload key, coalesce key) triple for its email so
    // callers never repeat it - and so the three can never drift out of sync. Newest-wins types key on
    // the user; the change notice is always-delivered and carries no key.

    /// <summary>Account-activation email for <paramref name="userId"/>. Newest activation per user wins.</summary>
    public static EmailOutboxMessage ForAccountActivation(Guid userId, string recipient, string? recipientName) =>
        Create(EmailType.AccountActivation, recipient, recipientName,
            new Dictionary<string, string> { [EmailOutboxPayloadKeys.UserId] = userId.ToString() },
            EmailOutboxCoalesceKeys.AccountActivation(userId));

    /// <summary>Password-reset email for reset <paramref name="passwordResetId"/>. Newest reset per user wins.</summary>
    public static EmailOutboxMessage ForPasswordReset(Guid passwordResetId, Guid userId, string recipient, string? recipientName) =>
        Create(EmailType.PasswordReset, recipient, recipientName,
            new Dictionary<string, string> { [EmailOutboxPayloadKeys.PasswordResetId] = passwordResetId.ToString() },
            EmailOutboxCoalesceKeys.PasswordReset(userId));

    /// <summary>Email-change verification for change <paramref name="emailChangeId"/>. Newest change per user wins.</summary>
    public static EmailOutboxMessage ForEmailVerification(Guid emailChangeId, Guid userId, string recipient, string? recipientName) =>
        Create(EmailType.EmailVerification, recipient, recipientName,
            new Dictionary<string, string> { [EmailOutboxPayloadKeys.EmailChangeId] = emailChangeId.ToString() },
            EmailOutboxCoalesceKeys.EmailVerification(userId));

    /// <summary>Informational notice to a previous address that the email is being changed. Always delivered (no coalescing).</summary>
    public static EmailOutboxMessage ForEmailChangeNotice(string newEmail, string recipient, string? recipientName) =>
        Create(EmailType.EmailChangeNotice, recipient, recipientName,
            new Dictionary<string, string> { [EmailOutboxPayloadKeys.NewEmail] = newEmail });

    /// <summary>
    /// A preview/test email of the given <paramref name="type"/>, enqueued by an admin to verify the
    /// provider end to end. Carries only the <see cref="EmailOutboxPayloadKeys.Preview"/> flag: the
    /// delivery dispatcher renders that type's template with placeholder data and a dummy link rather
    /// than touching any request row or minting a token. Always delivered (no coalesce key), so every
    /// test send goes out on its own.
    /// </summary>
    public static EmailOutboxMessage ForPreview(EmailType type, string recipient, string? recipientName) =>
        Create(type, recipient, recipientName,
            new Dictionary<string, string> { [EmailOutboxPayloadKeys.Preview] = "true" });
}
