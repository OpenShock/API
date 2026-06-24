using OpenShock.Common.Services.Email.Mailjet.Mail;

namespace OpenShock.Common.Services.Email;

/// <summary>
/// Low-level email transport. Implementations (Mailjet, SMTP, none) render a template and hand it to
/// the upstream provider. A failed delivery must surface as an <see cref="EmailDeliveryException"/>
/// so callers (notably the retry-queueing decorator) can decide whether to retry.
///
/// This is the raw sender, it has no knowledge of the retry queue. Application code should instead
/// depend on the queueing email service, which wraps this with queue-on-failure behaviour.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Account activation email sent when a user signs up.
    /// </summary>
    public Task ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Password reset email.
    /// </summary>
    public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Email-verification email sent when a user changes their email address.
    /// </summary>
    public Task VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Informational notice sent to the previous email address when an email change is initiated.
    /// </summary>
    public Task EmailChangeNotice(Contact to, string newEmail, CancellationToken cancellationToken = default);
}
