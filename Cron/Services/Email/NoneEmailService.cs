using OpenShock.Cron.Services.Email.Mailjet.Mail;

namespace OpenShock.Cron.Services.Email;

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
    
    // Returns Sent (not a failure) on purpose: in a deployment with mail disabled the outbox should
    // mark messages terminal rather than retrying them forever.
    public Task<EmailSendResult> ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Account activation email not sent, this is a noop implementation of the email service");
        return Task.FromResult(EmailSendResult.Sent);
    }

    public Task<EmailSendResult> PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Password reset email not sent, this is a noop implementation of the email service");
        return Task.FromResult(EmailSendResult.Sent);
    }

    public Task<EmailSendResult> VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Email verification email not sent, this is a noop implementation of the email service");
        return Task.FromResult(EmailSendResult.Sent);
    }

    public Task<EmailSendResult> EmailChangeNotice(Contact to, string newEmail, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Email change notice not sent, this is a noop implementation of the email service");
        return Task.FromResult(EmailSendResult.Sent);
    }
}