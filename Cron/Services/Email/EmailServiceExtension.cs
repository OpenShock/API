using OpenShock.Cron.Options;
using OpenShock.Cron.Services.Email.Mailjet;
using OpenShock.Cron.Services.Email.Outbox;
using OpenShock.Cron.Services.Email.Smtp;

namespace OpenShock.Cron.Services.Email;

public static class EmailServiceExtension
{
    public static async Task<WebApplicationBuilder> AddEmailService(this WebApplicationBuilder builder)
    {
        var mailOptions = builder.Configuration.GetRequiredSection(MailOptions.SectionName).Get<MailOptions>() ?? throw new NullReferenceException();

        // The outbox dispatcher + consumer drive all transactional email regardless of provider; even
        // with mail disabled the consumer runs and the send job marks messages terminal (no-op provider).
        builder.Services.AddSingleton<IEmailOutboxDispatcher, EmailOutboxDispatcher>();
        builder.Services.AddScoped<EmailOutboxJob>();
        builder.Services.AddHostedService<EmailOutboxConsumer>();

        if (mailOptions.Type == MailOptions.MailType.None)
        {
            builder.Services.AddSingleton<IEmailService, NoneEmailService>(); // Add a dummy email service
            return builder;
        }

        // Add sender contact configuration
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
