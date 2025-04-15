using OpenShock.API.Options;
using OpenShock.API.Services.Email.Mailjet;
using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.API.Services.Email.Smtp;

namespace OpenShock.API.Services.Email;

public static class EmailServiceExtension
{
    public static WebApplicationBuilder AddEmailService(this WebApplicationBuilder builder)
    {
        var mailOptions = builder.Configuration.GetRequiredSection(MailOptions.SectionName).Get<MailOptions>() ?? throw new NullReferenceException();
        
        if(mailOptions.Type == MailOptions.MailType.None)
        {
            builder.Services.AddSingleton<IEmailService, NoneEmailService>(); // Add a dummy email service
            return builder;
        }

        // Add sender contact configuration
        builder.AddSenderContactConfiguration();
        
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
        builder.Services.Configure<MailOptions.MailSenderContact>(MailOptions.SenderSectionName,
            builder.Configuration.GetRequiredSection(MailOptions.SectionName));
        return builder;
    }
}
