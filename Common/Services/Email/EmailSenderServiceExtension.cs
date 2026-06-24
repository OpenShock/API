using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Email.Mailjet;
using OpenShock.Common.Services.Email.Smtp;

namespace OpenShock.Common.Services.Email;

public static class EmailSenderServiceExtension
{
    /// <summary>
    /// Registers the raw <see cref="IEmailSender"/> (Mailjet / SMTP / none) plus the shared email
    /// configuration (sender contact, rendered templates, retry-queue tuning). Used by every host that
    /// sends mail — the API send path and the Cron retry worker.
    /// </summary>
    public static async Task<WebApplicationBuilder> AddOpenShockEmailSender(this WebApplicationBuilder builder)
    {
        var mailOptions = builder.Configuration.GetRequiredSection(MailOptions.SectionName).Get<MailOptions>() ?? throw new NullReferenceException();

        // Always available so the queueing decorator and the retry processor can read it.
        builder.AddEmailQueueOptions();

        if (mailOptions.Type == MailOptions.MailType.None)
        {
            builder.Services.AddSingleton<IEmailSender, NoneEmailService>(); // Add a dummy email sender
            return builder;
        }

        builder.AddSenderContactConfiguration();
        await builder.AddEmailServiceTemplates();

        switch (mailOptions.Type)
        {
            case MailOptions.MailType.Mailjet:
                builder.AddMailjetEmailService();
                break;
            case MailOptions.MailType.Smtp:
                builder.AddSmtpEmailService();
                break;
            default:
                throw new Exception("Unknown mail type");
        }

        return builder;
    }

    private static WebApplicationBuilder AddSenderContactConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(builder.Configuration.GetRequiredSection(MailOptions.SenderSectionName).Get<MailOptions.MailSenderContact>() ?? throw new NullReferenceException());
        return builder;
    }

    private static WebApplicationBuilder AddEmailQueueOptions(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(EmailQueueOptions.SectionName).Get<EmailQueueOptions>() ?? new EmailQueueOptions();
        builder.Services.AddSingleton(options);
        return builder;
    }

    private static async Task<WebApplicationBuilder> AddEmailServiceTemplates(this WebApplicationBuilder builder)
    {
        var accountActivation = EmailTemplate.ParseFromFileThrow("SmtpTemplates/AccountActivation.liquid");
        var passwordReset = EmailTemplate.ParseFromFileThrow("SmtpTemplates/PasswordReset.liquid");
        var emailVerification = EmailTemplate.ParseFromFileThrow("SmtpTemplates/EmailVerification.liquid");
        var emailChangeNotice = EmailTemplate.ParseFromFileThrow("SmtpTemplates/EmailChangeNotice.liquid");

        await Task.WhenAll(accountActivation, passwordReset, emailVerification, emailChangeNotice);

        builder.Services.AddSingleton(new EmailServiceTemplates
        {
            AccountActivation = await accountActivation,
            PasswordReset = await passwordReset,
            EmailVerification = await emailVerification,
            EmailChangeNotice = await emailChangeNotice,
        });
        return builder;
    }
}
