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
    public Task PasswordReset(string email, string name, Uri resetLink);
}