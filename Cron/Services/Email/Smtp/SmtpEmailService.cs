using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using OpenShock.Cron.Options;
using OpenShock.Cron.Services.Email.Mailjet.Mail;

namespace OpenShock.Cron.Services.Email.Smtp;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailServiceTemplates _templates;
    private readonly SmtpOptions _options;
    private readonly MailboxAddress _sender;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
            EmailServiceTemplates templates,
            SmtpOptions options,
            MailSenderContact sender,
            ILogger<SmtpEmailService> logger
        )
    {
        _templates = templates;
        _options = options;
        _sender = sender.ToMailAddress();
        _logger = logger;
    }

    public Task<EmailSendResult> ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
        => SendMail(to, _templates.AccountActivation, new { To = to, ActivationLink = activationLink }, cancellationToken);

    /// <inheritdoc />
    public Task<EmailSendResult> PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
        => SendMail(to, _templates.PasswordReset, new { To = to, ResetLink = resetLink }, cancellationToken);

    /// <inheritdoc />
    public Task<EmailSendResult> VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default)
        => SendMail(to, _templates.EmailVerification, new { To = to, VerifyLink = verificationLink }, cancellationToken);

    /// <inheritdoc />
    public Task<EmailSendResult> EmailChangeNotice(Contact to, string newEmail, CancellationToken cancellationToken = default)
        => SendMail(to, _templates.EmailChangeNotice, new { To = to, NewEmail = newEmail }, cancellationToken);

    private async Task<EmailSendResult> SendMail<T>(Contact to, EmailTemplate template, T data, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending email");
        var (subject, htmlBody) = await template.RenderAsync(data);

        var message = new MimeMessage
        {
            From = { _sender },
            Sender = _sender,
            To = { to.ToMailAddress() },
            Subject = subject,
            Body = new TextPart(TextFormat.Html) { Text = htmlBody }
        };

        try
        {
            _logger.LogTrace("Creating smtp client and connecting...");
            using var smtpClient = new SmtpClient();
            if (!_options.VerifyCertificate)
            {
                smtpClient.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                smtpClient.CheckCertificateRevocation = false;
            }

            await smtpClient.ConnectAsync(_options.Host, _options.Port, _options.EnableSsl, cancellationToken);
            _logger.LogTrace("Authenticating...");
            if (smtpClient.Capabilities.HasFlag(SmtpCapabilities.Authentication))
                await smtpClient.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

            _logger.LogTrace("Smtp client connected, sending email...");

            await smtpClient.SendAsync(message, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);
            _logger.LogTrace("Sent email");

            return EmailSendResult.Sent;
        }
        catch (SmtpCommandException ex)
        {
            // A 5xx reply (e.g. mailbox unavailable, message rejected) won't be fixed by retrying;
            // 4xx replies are temporary.
            var permanent = (int)ex.StatusCode is >= 500 and <= 599;
            // Don't log the recipient address - the outbox row (keyed by message id) already records it.
            _logger.LogError(ex, "SMTP command failed with status {StatusCode}", ex.StatusCode);
            return permanent ? EmailSendResult.PermanentFailure : EmailSendResult.TransientFailure;
        }
        catch (Exception ex)
        {
            // Connection, TLS, auth, protocol and timeout failures are all treated as temporary; the
            // retry budget bounds how long a genuinely broken configuration keeps being attempted.
            // Don't log the recipient address - the outbox row (keyed by message id) already records it.
            _logger.LogError(ex, "Transient SMTP failure while sending email");
            return EmailSendResult.TransientFailure;
        }
    }
}