using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email;

/// <summary>
/// This is a noop implementation of the email service. It does nothing.
/// Consumers should properly handle when this service is used, so realistaically this should never be used.
/// But we need it for DI satisfaction.
/// </summary>
public class NoneEmailService : IEmailService
{
    public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task VerifyEmail(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}