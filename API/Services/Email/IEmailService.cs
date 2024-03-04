using OneOf;
using OneOf.Types;
using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email;

public interface IEmailService
{
    /// <summary>
    /// Send a password reset email
    /// </summary>
    /// <param name="to"></param>
    /// <param name="resetLink"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// When a user uses the signup form we send this email to let them activate their email
    /// </summary>
    /// <param name="to"></param>
    /// <param name="activationLink"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task ActivateAccountEmail(Contact to, Uri activationLink, CancellationToken cancellationToken = default);
}
