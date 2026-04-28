using Microsoft.Extensions.Options;
using OpenShock.API.Options;

namespace OpenShock.API.Services.Email.Smtp;

public static class SmtpEmailServiceExtension
{
    public static WebApplicationBuilder AddSmtpEmailService(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection(SmtpOptions.SectionName);

        // TODO Simplify this
        builder.Services.Configure<SmtpOptions>(section);
        builder.Services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();
        builder.Services.AddSingleton<SmtpOptions>(sp => sp.GetRequiredService<IOptions<SmtpOptions>>().Value);

        builder.Services.AddSingleton(new SmtpServiceTemplates
        {
            AccountActivation = SmtpTemplate.ParseFromFileThrow("SmtpTemplates/AccountActivation.liquid").Result,
            PasswordReset = SmtpTemplate.ParseFromFileThrow("SmtpTemplates/PasswordReset.liquid").Result,
            EmailVerification = SmtpTemplate.ParseFromFileThrow("SmtpTemplates/EmailVerification.liquid").Result
        });

        builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

        return builder;
    }
}