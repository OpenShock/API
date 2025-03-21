using OpenShock.API.Options;
using OpenShock.API.Services.Email.Mailjet;
using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.API.Services.Email.Smtp;
using System.Configuration;

namespace OpenShock.API.Services.Email;

public static class EmailServiceExtension
{
    public static WebApplicationBuilder AddEmailService(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<Contact>(MailOptions.SenderOptionName, builder.Configuration.GetRequiredSection(MailOptions.SenderSectionName));

        var mailOptions = builder.Configuration.GetRequiredSection(MailOptions.SectionName).Get<MailOptions>() ?? throw new NullReferenceException();
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
}
