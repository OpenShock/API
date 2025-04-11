using Microsoft.Extensions.Options;
using OpenShock.API.Options;

namespace OpenShock.API.Services.Email.Smtp;

public static class SmtpEmailServiceExtension
{
    public static WebApplicationBuilder AddSmtpEmailService(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection(SmtpOptions.SectionName);

        builder.Services.Configure<SmtpOptions>(section);
        builder.Services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();

        builder.Services.AddSingleton(new SmtpServiceTemplates
        {
            PasswordReset = SmtpTemplate.ParseFromFileThrow("SmtpTemplates/PasswordReset.liquid").Result,
            EmailVerification = SmtpTemplate.ParseFromFileThrow("SmtpTemplates/EmailVerification.liquid").Result
        });

        builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

        return builder;
    }
}