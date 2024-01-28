using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email;

public interface IEmailService
{
    /// <summary>
    /// Send a password reset email
    /// </summary>
    /// <param name="email"></param>
    /// <param name="name"></param>
    /// <param name="resetLink"></param>
    /// <returns></returns>
    public Task PasswordReset(Contact to, Uri resetLink);
}