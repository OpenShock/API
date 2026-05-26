using OpenShock.API.Options;
using OpenShock.API.Services.Email.Mailjet;
using OpenShock.API.Services.Email.Smtp;

namespace OpenShock.API.Services.Email;

public static class EmailServiceExtension
{
    public static async Task<WebApplicationBuilder> AddEmailService(this WebApplicationBuilder builder)
    {
        var mailOptions = builder.Configuration.GetRequiredSection(MailOptions.SectionName).Get<MailOptions>() ?? throw new NullReferenceException();
        
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
