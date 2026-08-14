using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.Options;

namespace OpenShock.API.IntegrationTests;

/// <summary>
/// Variant of <see cref="WebApplicationFactory"/> that runs with no mail provider configured.
/// </summary>
public sealed class MailDisabledWebApplicationFactory : WebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(MailOptions));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton(new MailOptions { Type = MailOptions.MailType.None });
        });
    }
}
