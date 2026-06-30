namespace OpenShock.Cron.Services.Email;

/// <summary>
/// Outcome of a single attempt to hand an email to the provider. Drives the outbox consumer's
/// retry decision, so implementations must classify provider failures rather than swallowing them.
/// </summary>
public enum EmailSendResult
{
    /// <summary>The provider accepted the message. Terminal success.</summary>
    Sent,

    /// <summary>
    /// A temporary failure (timeout, network error, provider 5xx, rate-limit 429). The same send
    /// should be retried later.
    /// </summary>
    TransientFailure,

    /// <summary>
    /// A failure that retrying will not fix (e.g. provider 4xx other than 429, malformed/rejected
    /// recipient). The message should not be retried.
    /// </summary>
    PermanentFailure
}
