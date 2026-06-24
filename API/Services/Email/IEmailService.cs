using OpenShock.Common.Services.Email;
using OpenShock.Common.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email;

/// <summary>
/// Application-facing email service. Sends now via the underlying <see cref="IEmailSender"/> and, when
/// the upstream provider fails transiently, queues the email for a later retry instead of throwing.
///
/// Queueable methods take the target <c>userId</c> so the retry worker can look the account up and
/// regenerate a fresh token before resending — tokens are never persisted in the queue.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// When a user uses the signup form we send this email to let them activate their account.
    /// </summary>
    /// <param name="userId">The id of the account being activated (used to regenerate the link on retry).</param>
    /// <param name="to"></param>
    /// <param name="activationLink"></param>
    /// <param name="cancellationToken"></param>
    public Task ActivateAccount(Guid userId, Contact to, Uri activationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a password reset email. Not retry-queued: a failed reset can simply be re-requested.
    /// </summary>
    /// <param name="to"></param>
    /// <param name="resetLink"></param>
    /// <param name="cancellationToken"></param>
    public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// When a user changes their email, we send them this email to let them verify it.
    /// </summary>
    /// <param name="userId">The id of the account whose email is being changed.</param>
    /// <param name="to"></param>
    /// <param name="verificationLink"></param>
    /// <param name="cancellationToken"></param>
    public Task VerifyEmail(Guid userId, Contact to, Uri verificationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Informational notice sent to the user's previous email address when an email change is
    /// initiated. Contains no action link — its only purpose is to alert the legitimate owner
    /// of the address that a change request was started, in case the account was compromised.
    /// </summary>
    /// <param name="userId">The id of the account whose email is being changed.</param>
    /// <param name="to">The old email address being notified.</param>
    /// <param name="newEmail">The new email address that was requested.</param>
    /// <param name="cancellationToken"></param>
    public Task EmailChangeNotice(Guid userId, Contact to, string newEmail, CancellationToken cancellationToken = default);
}
