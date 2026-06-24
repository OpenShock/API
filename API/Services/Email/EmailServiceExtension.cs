using OpenShock.API.Services.Email.Queue;
using OpenShock.Common.Services.Email;

namespace OpenShock.API.Services.Email;

public static class EmailServiceExtension
{
    public static async Task<WebApplicationBuilder> AddEmailService(this WebApplicationBuilder builder)
    {
        // Shared raw sender + email configuration (Mailjet / SMTP / none).
        await builder.AddOpenShockEmailSender();

        // Application-facing send path: send now, queue on transient failure. The Cron project owns
        // the worker that drains the queue, so no hosted service is registered here.
        builder.Services.AddScoped<IEmailService, QueueingEmailService>();

        return builder;
    }
}
