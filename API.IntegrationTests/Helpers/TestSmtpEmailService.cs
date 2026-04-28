using System.Text.Encodings.Web;
using Fluid;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using OpenShock.API.Options;
using OpenShock.API.Services.Email;
using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.API.Services.Email.Smtp;

namespace OpenShock.API.IntegrationTests.Helpers;

/// <summary>
/// An <see cref="IEmailService"/> implementation used in integration tests that sends emails
/// synchronously (no fire-and-forget) so that the API endpoint returns only after the email
/// has been handed off to Mailpit. This makes Mailpit assertions deterministic.
/// </summary>
internal sealed class TestSmtpEmailService : IEmailService
{
    private readonly SmtpServiceTemplates _templates;
    private readonly SmtpOptions _options;
    private readonly MailboxAddress _sender;
    private readonly TemplateOptions _templateOptions;

    public TestSmtpEmailService(
        SmtpServiceTemplates templates,
        SmtpOptions options,
        MailOptions.MailSenderContact sender)
    {
        _templates = templates;
        _options = options;
        _sender = new MailboxAddress(sender.Name, sender.Email);

        _templateOptions = new TemplateOptions();
        _templateOptions.MemberAccessStrategy.Register<Contact>();
    }

    public Task ActivateAccount(Contact to, Uri activationLink, CancellationToken cancellationToken = default)
        => SendMailAsync(to, _templates.AccountActivation, new { To = to, ActivationLink = activationLink }, cancellationToken);

    public Task PasswordReset(Contact to, Uri resetLink, CancellationToken cancellationToken = default)
        => SendMailAsync(to, _templates.PasswordReset, new { To = to, ResetLink = resetLink }, cancellationToken);

    public Task VerifyEmail(Contact to, Uri verificationLink, CancellationToken cancellationToken = default)
        => SendMailAsync(to, _templates.EmailVerification, new { To = to, ActivationLink = verificationLink }, cancellationToken);

    private async Task SendMailAsync<T>(Contact to, SmtpTemplate template, T data, CancellationToken cancellationToken)
    {
        var context = new TemplateContext(data, _templateOptions);
        var subject = await template.Subject.RenderAsync(context);

        var buffer = new MemoryStream();
        await using (var writer = new StreamWriter(buffer, leaveOpen: true))
            await template.Body.RenderAsync(writer, HtmlEncoder.Default, context);

        buffer.Position = 0;

        var message = new MimeMessage
        {
            From = { _sender },
            Sender = _sender,
            To = { new MailboxAddress(to.Name, to.Email) },
            Subject = subject,
            Body = new TextPart(TextFormat.Html) { Content = new MimeContent(buffer) }
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_options.Host, _options.Port, _options.EnableSsl, cancellationToken);
        if (!string.IsNullOrEmpty(_options.Username))
            await smtp.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        await smtp.SendAsync(message, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }
}
