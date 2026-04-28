using OpenShock.API.Options;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace OpenShock.API.Services.Email.Mailjet;

public static class MailjetEmailServiceExtension
{
    public static WebApplicationBuilder AddMailjetEmailService(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection(MailJetOptions.SectionName);

        
        // TODO Simplify this
        builder.Services.Configure<MailJetOptions>(section);
        builder.Services.AddSingleton<IValidateOptions<MailJetOptions>, MailJetOptionsValidator>();
        builder.Services.AddSingleton<MailJetOptions>(sp => sp.GetRequiredService<IOptions<MailJetOptions>>().Value);

        var options = section.Get<MailJetOptions>() ?? throw new NullReferenceException("MailJetOptions is null!");
        var basicAuthValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Key}:{options.Secret}"));

        builder.Services.AddHttpClient<IEmailService, MailjetEmailService>(httpclient =>
        {
            httpclient.BaseAddress = new Uri("https://api.mailjet.com/v3.1/");
            httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthValue);
        });

        return builder;
    }
}