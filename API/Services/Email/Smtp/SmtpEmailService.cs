using System.Net;
using System.Net.Mail;
using Fluid;
using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.Common.Utils;

namespace OpenShock.API.Services.Email.Smtp;

public sealed class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly ApiConfig.MailConfig.SmtpConfig _smtpConfig;
    private readonly SmtpServiceTemplates _templates;
    private readonly MailAddress _sender;

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

    private async Task SendMail<T>(Contact to, SmtpTemplate template, T data)
    {
        _logger.LogDebug("Start sending email");
        using var smtpClient = new SmtpClient(_smtpConfig.Host, _smtpConfig.Port);
        smtpClient.Credentials = new NetworkCredential(_smtpConfig.Username, _smtpConfig.Password);
        smtpClient.EnableSsl = _smtpConfig.EnableSsl;
        ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;

        var context = new TemplateContext(data);
        
        var subject = await template.Subject.RenderAsync(context);
        var body = await template.Body.RenderAsync(context);
        
        var mailMessage = new MailMessage
        {
            From = _sender,
            Sender = _sender,
            To = { to.ToMailAddress() },
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        _logger.LogDebug("Sending email");
        await smtpClient.SendMailAsync(mailMessage);
        _logger.LogDebug("Email sent");
    }

    private void SendMailAndForget<T>(Contact to, SmtpTemplate template, T data)
    {
        LucTask.Run(SendMail(to, template, data));
    }
    
    public Task PasswordReset(Contact to, Uri resetLink)
    {
        var data = new
        {
            To = to,
            ResetLink = resetLink
        };
        
        SendMailAndForget(to, _templates.PasswordReset, data);
        return Task.CompletedTask;
    }
}