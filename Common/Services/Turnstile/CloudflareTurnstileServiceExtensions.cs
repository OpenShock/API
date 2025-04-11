using Microsoft.Extensions.Options;
using OpenShock.Common.Options;

namespace OpenShock.Common.Services.Turnstile;

public static class CloudflareTurnstileServiceExtensions
{
    public static WebApplicationBuilder AddCloudflareTurnstileService(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection(CloudflareTurnstileOptions.Turnstile);

        builder.Services.Configure<CloudflareTurnstileOptions>(section);
        builder.Services.AddSingleton<IValidateOptions<CloudflareTurnstileOptions>, CloudflareTurnstileOptionsValidator>();

        builder.Services.AddHttpClient<ICloudflareTurnstileService, CloudflareTurnstileService>(client =>
        {
            client.BaseAddress = new Uri("https://challenges.cloudflare.com/turnstile/v0/");
        });

        return builder;
    }
}
