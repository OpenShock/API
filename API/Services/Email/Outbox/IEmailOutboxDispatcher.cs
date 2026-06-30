using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Services.Email.Outbox;

/// <summary>
/// Renders and sends a single <see cref="EmailOutboxMessage"/>. This is the one place email is
/// actually sent: it maps the message's type and dynamic payload to a template, lazily mints (and
/// persists the hash of) any secret token the email needs, hands the message to
/// <see cref="IEmailService"/>, and reports the outcome. It does not own the message's lifecycle —
/// the consumer that calls it records the resulting state transition.
/// </summary>
public interface IEmailOutboxDispatcher
{
    /// <summary>
    /// Attempts to deliver <paramref name="message"/>. Any token row touched (e.g. a
    /// <see cref="UserPasswordReset"/>) is updated via <paramref name="db"/> and saved before the
    /// send, so the link in the email always matches a stored hash. Never throws for ordinary send
    /// failures — they are returned as <see cref="EmailDispatchResult"/>.
    /// </summary>
    /// <param name="message">The message to send. Its status is not modified here.</param>
    /// <param name="db">The context used to load/update related request rows.</param>
    /// <param name="cancellationToken"></param>
    Task<EmailDispatchResult> SendAsync(EmailOutboxMessage message, OpenShockContext db, CancellationToken cancellationToken = default);
}
