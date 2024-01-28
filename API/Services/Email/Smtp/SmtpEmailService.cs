using System.Net;
using System.Net.Mail;
using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email.Smtp;

public sealed class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly ApiConfig.MailConfig.SmtpConfig _smtpConfig;
    private readonly MailAddress _sender;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="smtpConfig"></param>
    /// <param name="sender"></param>
    public SmtpEmailService(ILogger<SmtpEmailService> logger,
        ApiConfig.MailConfig.SmtpConfig smtpConfig,
        Contact sender)
    {
        _logger = logger;
        _smtpConfig = smtpConfig;
        _sender = sender.ToMailAddress();
    }

    private async Task SendMail(Contact to, string subject, string body)
    {
        using var smtpClient = new SmtpClient(_smtpConfig.Server, _smtpConfig.Port);
        smtpClient.Credentials = new NetworkCredential(_smtpConfig.Username, _smtpConfig.Password);
        smtpClient.EnableSsl = _smtpConfig.EnableSsl;

        var mailMessage = new MailMessage
        {
            From = _sender,
            Sender = _sender,
            To = { to.ToMailAddress() },
            Subject = subject,
            Body = body
        };

        await smtpClient.SendMailAsync(mailMessage);
    }

    public Task PasswordReset(Contact to, Uri resetLink)
    {
        return SendMail(to, "Password reset request", $"Click <a href=\"{resetLink}\">here</a> to reset your password");
    }
}