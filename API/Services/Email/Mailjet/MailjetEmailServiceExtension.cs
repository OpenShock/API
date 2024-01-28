using OpenShock.API.Services.Email.Mailjet.Mail;

namespace OpenShock.API.Services.Email.Mailjet;

public static class MailjetEmailServiceExtension
{
    public static IServiceCollection AddMailjetEmailService(this IServiceCollection services,
        ApiConfig.MailConfig.MailjetConfig config, Contact sender)
    {
        services.AddSingleton<IEmailService, MailjetEmailService>(provider =>
            new MailjetEmailService(provider.GetRequiredService<ILogger<MailjetEmailService>>(), config, sender));
        return services;
    }
}