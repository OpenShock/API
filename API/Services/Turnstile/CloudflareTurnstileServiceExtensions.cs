using Microsoft.Extensions.Options;
using OpenShock.API.Options;
using OpenShock.Common.Options;

namespace OpenShock.API.Services.Turnstile;

public static class CloudflareTurnstileServiceExtensions
{
    public static WebApplicationBuilder AddCloudflareTurnstileService(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection(TurnstileOptions.Turnstile);

        // TODO Simplify this
        builder.Services.Configure<TurnstileOptions>(section);
        builder.Services.AddSingleton<IValidateOptions<TurnstileOptions>, TurnstileOptionsValidator>();
        builder.Services.AddSingleton<TurnstileOptions>(sp => sp.GetRequiredService<IOptions<TurnstileOptions>>().Value);

        builder.Services.AddHttpClient<ICloudflareTurnstileService, CloudflareTurnstileService>(client =>
        {
            client.BaseAddress = new Uri("https://challenges.cloudflare.com/turnstile/v0/");
        });

        return builder;
    }
}
