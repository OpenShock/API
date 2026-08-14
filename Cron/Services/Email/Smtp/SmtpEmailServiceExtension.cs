using Microsoft.Extensions.Options;
using OpenShock.Cron.Options;

namespace OpenShock.Cron.Services.Email.Smtp;

public static class SmtpEmailServiceExtension
{
    public static WebApplicationBuilder AddSmtpEmailService(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection(SmtpOptions.SectionName);

        // TODO Simplify this
        builder.Services.Configure<SmtpOptions>(section);
        builder.Services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();
        builder.Services.AddSingleton<SmtpOptions>(sp => sp.GetRequiredService<IOptions<SmtpOptions>>().Value);

        builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

        return builder;
    }
}