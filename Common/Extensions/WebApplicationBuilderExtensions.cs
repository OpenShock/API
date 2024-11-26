using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

namespace OpenShock.Common.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void ApplyBaseConfiguration(this WebApplicationBuilder builder, Action<KestrelServerOptions> configurePorts)
    {
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddJsonFile("appsettings.Custom.json", optional: true, reloadOnChange: false);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

        var isDevelopment = builder.Environment.IsDevelopment();
        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateScopes = isDevelopment;
            options.ValidateOnBuild = isDevelopment;
        });
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            configurePorts(serverOptions);
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMilliseconds(3000);
        });

        builder.Host.UseSerilog((context, _, config) => config.ReadFrom.Configuration(context.Configuration));
    }
}
