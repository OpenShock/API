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
}
