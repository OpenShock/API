using Microsoft.Extensions.Options;
using OpenShock.Common.Options;

namespace OpenShock.Common.Services.Email.Smtp;

public static class SmtpEmailServiceExtension
{
    public static WebApplicationBuilder AddSmtpEmailService(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection(SmtpOptions.SectionName);

        // TODO Simplify this
        builder.Services.Configure<SmtpOptions>(section);
        builder.Services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();
        builder.Services.AddSingleton<SmtpOptions>(sp => sp.GetRequiredService<IOptions<SmtpOptions>>().Value);

        builder.Services.AddSingleton<IEmailSender, SmtpEmailService>();

        return builder;
    }
}