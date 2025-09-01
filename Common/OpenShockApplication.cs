using MessagePack;
using Serilog;

namespace OpenShock.Common;

public static class OpenShockApplication
{
    public static WebApplicationBuilder CreateDefaultBuilder<TProgram>(string[] args) where TProgram : class
    {
        MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.NativeGuidResolver.Instance);
        
        var builder = WebApplication.CreateSlimBuilder(args);
        
        builder.Configuration.Sources.Clear();
        if (Environment.GetEnvironmentVariable("ASPNETCORE_UNDER_INTEGRATION_TEST") == "1")
        {
            builder.Configuration
                .AddEnvironmentVariables();
        }
        else
        {
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.Container.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.Custom.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .AddUserSecrets<TProgram>(true)
                .AddCommandLine(args);
        }

        var isDevelopment = builder.Environment.IsDevelopment();
        builder.Host.UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateScopes = isDevelopment;
            options.ValidateOnBuild = isDevelopment;
        });

        // Since we use slim builders, this allows for HTTPS during local development
        builder.WebHost.UseKestrelHttpsConfiguration();
        
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMilliseconds(3000);
        });

        builder.Host.UseSerilog((context, _, config) => config.ReadFrom.Configuration(context.Configuration));
        
        return builder;
    }
}
