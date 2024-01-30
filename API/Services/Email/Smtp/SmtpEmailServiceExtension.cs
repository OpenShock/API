using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email.Smtp;

public static class SmtpEmailServiceExtension
{
    public static IServiceCollection AddSmtpEmailService(this IServiceCollection services,
        ApiConfig.MailConfig.SmtpConfig smtpConfig,
        Contact sender, SmtpServiceTemplates templates)
    {
        services.AddSingleton<IEmailService, SmtpEmailService>(provider =>
            new SmtpEmailService(provider.GetRequiredService<ILogger<SmtpEmailService>>(), smtpConfig, sender, templates));
        return services;
    }
}