using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Email.Mailjet.Mail;

namespace OpenShock.Common.Services.Email.Smtp;

public sealed class SmtpEmailService : IEmailSender
{
    private readonly EmailServiceTemplates _templates;
    private readonly SmtpOptions _options;
    private readonly MailboxAddress _sender;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
            EmailServiceTemplates templates,
            SmtpOptions options,
            MailOptions.MailSenderContact sender,
            ILogger<SmtpEmailService> logger
        )
    {
        _templates = templates;
        _options = options;
        _sender = sender.ToMailAddress();
        _logger = logger;
    }

    public Task ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
        => SendMail(to, _templates.AccountActivation, new { To = to, ActivationLink = activationLink }, cancellationToken);

    /// <inheritdoc />
    public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
        => SendMail(to, _templates.PasswordReset, new { To = to, ResetLink = resetLink }, cancellationToken);

    /// <inheritdoc />
    public Task VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default)
        => SendMail(to, _templates.EmailVerification, new { To = to, VerifyLink = verificationLink }, cancellationToken);

    /// <inheritdoc />
    public Task EmailChangeNotice(Contact to, string newEmail, CancellationToken cancellationToken = default)
        => SendMail(to, _templates.EmailChangeNotice, new { To = to, NewEmail = newEmail }, cancellationToken);

    private async Task SendMail<T>(Contact to, EmailTemplate template, T data, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending email");
        var (subject, htmlBody) = await template.RenderAsync(data);

        using var message = new MimeMessage
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
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SmtpCommandException ex)
        {
            // SMTP 5xx is a permanent rejection (bad recipient, message refused); 4xx is temporary
            // (mailbox busy, service not available) and worth retrying.
            var isTransient = (int)ex.StatusCode < 500;
            throw new EmailDeliveryException(isTransient, $"SMTP command failed ({ex.StatusCode}): {ex.Message}", ex);
        }
        catch (AuthenticationException ex)
        {
            throw new EmailDeliveryException(isTransient: false, "SMTP authentication failed", ex);
        }
        catch (Exception ex)
        {
            // Protocol errors, socket failures and timeouts are transient by nature.
            throw new EmailDeliveryException(isTransient: true, "Failed to send email over SMTP", ex);
        }
    }
}