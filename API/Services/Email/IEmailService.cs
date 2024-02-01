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
}