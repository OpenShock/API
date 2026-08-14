namespace OpenShock.Cron.Services.Email.Outbox;

/// <summary>What happened when the consumer tried to dispatch an outbox message.</summary>
public enum EmailDispatchOutcome
{
    /// <summary>Handed to the provider successfully. Terminal.</summary>
    Sent,

    /// <summary>Temporary failure; the message should be retried later.</summary>
    TransientFailure,

    /// <summary>Permanent failure; retrying will not help.</summary>
    PermanentFailure,

    /// <summary>
    /// Nothing was sent because the underlying request no longer needs the email (expired, already
    /// used, superseded, or gone). Terminal - there is nothing to retry.
    /// </summary>
    Skipped
}

/// <summary>Outcome of dispatching one <see cref="OpenShock.Common.OpenShockDb.EmailOutboxMessage"/>, with a human-readable detail for diagnostics.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Detail">Optional explanation, surfaced into the message's last-error field.</param>
public readonly record struct EmailDispatchResult(EmailDispatchOutcome Outcome, string? Detail)
{
    /// <summary>A successful send.</summary>
    public static readonly EmailDispatchResult Sent = new(EmailDispatchOutcome.Sent, null);

    /// <summary>A transient failure that should be retried.</summary>
    public static EmailDispatchResult Transient(string detail) => new(EmailDispatchOutcome.TransientFailure, detail);

    /// <summary>A permanent failure that should not be retried.</summary>
    public static EmailDispatchResult Permanent(string detail) => new(EmailDispatchOutcome.PermanentFailure, detail);

    /// <summary>The email was intentionally not sent because it is no longer needed.</summary>
    public static EmailDispatchResult Skip(string detail) => new(EmailDispatchOutcome.Skipped, detail);
}
