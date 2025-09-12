using System.Text.Encodings.Web;
using Fluid;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using OpenShock.API.Options;
using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.Common.Utils;

namespace OpenShock.API.Services.Email.Smtp;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpServiceTemplates _templates;
    private readonly SmtpOptions _options;
    private readonly MailboxAddress _sender;
    private readonly ILogger<SmtpEmailService> _logger;

    private readonly TemplateOptions _templateOptions;


    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="templates"></param>
    /// <param name="options"></param>
    /// <param name="sender"></param>
    /// <param name="logger"></param>
    public SmtpEmailService(
            SmtpServiceTemplates templates,
            IOptions<SmtpOptions> options,
            MailOptions.MailSenderContact sender,
            ILogger<SmtpEmailService> logger
        )
    {
        _templates = templates;
        _options = options.Value;
        _sender = sender.ToMailAddress();
        _logger = logger;

        // This class is will be registered as a singleton, static members are not needed
        _templateOptions = new TemplateOptions();
        _templateOptions.MemberAccessStrategy.Register<Contact>();
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
        OsTask.Run(() => SendMail(to, template, data, cancellationToken));


    private async Task SendMail<T>(Contact to, SmtpTemplate template, T data,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending email");
        var context = new TemplateContext(data, _templateOptions);
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
        if (!_options.VerifyCertificate)
        {
            smtpClient.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            smtpClient.CheckCertificateRevocation = false;
        }

        await smtpClient.ConnectAsync(_options.Host, _options.Port, _options.EnableSsl, cancellationToken);
        _logger.LogTrace("Authenticating...");
        await smtpClient.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

        _logger.LogTrace("Smtp client connected, sending email...");

        await smtpClient.SendAsync(message, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);
        _logger.LogTrace("Sent email");
    }
}