using Fluid;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.Common.Utils;
using System.Text.Encodings.Web;

namespace OpenShock.API.Services.Email.Smtp;

public sealed class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly ApiConfig.MailConfig.SmtpConfig _smtpConfig;
    private readonly SmtpServiceTemplates _templates;
    private readonly MailboxAddress _sender;
    private static readonly TemplateOptions TemplateOptions;

    static SmtpEmailService()
    {
        TemplateOptions = new TemplateOptions();
        TemplateOptions.MemberAccessStrategy.Register<Contact>();
    }

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="smtpConfig"></param>
    /// <param name="sender"></param>
    /// <param name="templates"></param>
    public SmtpEmailService(ILogger<SmtpEmailService> logger,
        ApiConfig.MailConfig.SmtpConfig smtpConfig,
        Contact sender, SmtpServiceTemplates templates)
    {
        _logger = logger;

        _smtpConfig = smtpConfig;
        _templates = templates;
        _sender = sender.ToMailAddress();
    }

    /// <inheritdoc />
    public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
    {
        var data = new
        {
            To = to,
            ResetLink = resetLink
        };

        SendMailAndForget(to, _templates.PasswordReset, data, cancellationToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task VerifyEmail(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
    {
        var data = new
        {
            To = to,
            ActivationLink = activationLink
        };

        SendMailAndForget(to, _templates.EmailVerification, data, cancellationToken);
        return Task.CompletedTask;
    }

    private void SendMailAndForget<T>(Contact to, SmtpTemplate template, T data,
        CancellationToken cancellationToken = default) =>
        LucTask.Run(() => SendMail(to, template, data, cancellationToken));


    private async Task SendMail<T>(Contact to, SmtpTemplate template, T data,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending email");
        var context = new TemplateContext(data, TemplateOptions);
        var subject = await template.Subject.RenderAsync(context);

        await using var buffer = new MemoryStream();
        await using (var textStreamWriter = new StreamWriter(buffer, leaveOpen: true))
            await template.Body.RenderAsync(textStreamWriter, HtmlEncoder.Default, context);

        var message = new MimeMessage
        {
            From = { _sender },
            Sender = _sender,
            To = { to.ToMailAddress() },
            Subject = subject,
            Body = new TextPart(TextFormat.Html)
            {
                Content = new MimeContent(buffer)
            }
        };

        _logger.LogTrace("Creating smtp client and connecting...");
        using var smtpClient = new SmtpClient();
        if (!_smtpConfig.VerifyCertificate)
        {
            smtpClient.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            smtpClient.CheckCertificateRevocation = false;
        }

        await smtpClient.ConnectAsync(_smtpConfig.Host, _smtpConfig.Port, _smtpConfig.EnableSsl, cancellationToken);
        _logger.LogTrace("Authenticating...");
        await smtpClient.AuthenticateAsync(_smtpConfig.Username, _smtpConfig.Password, cancellationToken);

        _logger.LogTrace("Smtp client connected, sending email...");

        await smtpClient.SendAsync(message, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);
        _logger.LogTrace("Sent email");
    }
}