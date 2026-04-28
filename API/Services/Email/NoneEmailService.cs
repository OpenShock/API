using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email;

/// <summary>
/// This is a noop implementation of the email service. It does nothing.
/// Consumers should properly handle when this service is used, so realistaically this should never be used.
/// But we need it for DI satisfaction.
/// </summary>
public class NoneEmailService : IEmailService
{
    private readonly ILogger<NoneEmailService> _logger;

    public NoneEmailService(ILogger<NoneEmailService> logger)
    {
        _logger = logger;
    }
    
    public Task ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Account activation email not sent, this is a noop implementation of the email service");
        return Task.CompletedTask;
    }

    public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Password reset email not sent, this is a noop implementation of the email service");
        return Task.CompletedTask;
    }

    public Task VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Email verification email not sent, this is a noop implementation of the email service");
        return Task.CompletedTask;
    }
}