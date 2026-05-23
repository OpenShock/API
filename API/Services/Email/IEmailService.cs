using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email;

public interface IEmailService
{
    /// <summary>
    /// When a user uses the signup form we send this email to let them activate their account
    /// </summary>
    /// <param name="to"></param>
    /// <param name="activationLink"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a password reset email
    /// </summary>
    /// <param name="to"></param>
    /// <param name="resetLink"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// When a user uses changes their email, we send them this email to let them verify it
    /// </summary>
    /// <param name="to"></param>
    /// <param name="verificationLink"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Informational notice sent to the user's previous email address when an email change is
    /// initiated. Contains no action link — its only purpose is to alert the legitimate owner
    /// of the address that a change request was started, in case the account was compromised.
    /// </summary>
    /// <param name="to">The old email address being notified.</param>
    /// <param name="newEmail">The new email address that was requested.</param>
    /// <param name="cancellationToken"></param>
    public Task EmailChangeNotice(Contact to, string newEmail, CancellationToken cancellationToken = default);
}
