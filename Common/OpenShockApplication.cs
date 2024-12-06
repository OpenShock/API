using System.Reflection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

namespace OpenShock.Common;

public static class OpenShockApplication
{
    public static WebApplicationBuilder CreateDefaultBuilder<TProgram>(string[] args, Action<KestrelServerOptions> configurePorts) where TProgram : class
    {
        var builder = WebApplication.CreateSlimBuilder(args);
        
        builder.Configuration.Sources.Clear();
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Custom.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddUserSecrets<TProgram>(true)
            .AddCommandLine(args);

        var isDevelopment = builder.Environment.IsDevelopment();
        builder.Host.UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateScopes = isDevelopment;
            options.ValidateOnBuild = isDevelopment;
        });

        // Since we use slim builders, this allows for HTTPS during local development
        if (isDevelopment) builder.WebHost.UseKestrelHttpsConfiguration();
        
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            configurePorts(serverOptions);
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMilliseconds(3000);
        });

        builder.Host.UseSerilog((context, _, config) => config.ReadFrom.Configuration(context.Configuration));
        
        return builder;
    }
}
