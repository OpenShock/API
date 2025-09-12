using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Scalar.AspNetCore;
using Serilog;

namespace OpenShock.Common;

public static class OpenShockMiddlewareHelper
{
    private static readonly ForwardedHeadersOptions ForwardedSettings = new()
    {
        ForwardedHeaders = ForwardedHeaders.All,
        RequireHeaderSymmetry = false,
        ForwardLimit = null,
        ForwardedForHeaderName = "CF-Connecting-IP"
    };
    
    public static async Task<IApplicationBuilder> UseCommonOpenShockMiddleware(this WebApplication app)
    {
        var metricsOptions = app.Services.GetRequiredService<MetricsOptions>();
        var metricsAllowedIpNetworks = metricsOptions.AllowedNetworks.Select(x => IPNetwork.Parse(x)).ToArray();

        foreach (var proxy in await TrustedProxiesFetcher.GetTrustedNetworksAsync())
        {
            ForwardedSettings.KnownNetworks.Add(proxy);
        }

        app.UseForwardedHeaders(ForwardedSettings);
        
        app.UseSerilogRequestLogging();

        // We will only log request body in development
        if (app.Environment.IsDevelopment())
        {
            // Enable request body buffering. Needed to allow rewinding the body reader,
            // if the body has already been read before.
            // Runs before the request action is executed and body is read.
            app.Use((context, next) =>
            {
                context.Request.EnableBuffering();
                return next.Invoke();
            });
        }
        app.UseExceptionHandler();

        // global cors policy
        app.UseCors();
        
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1)
        });
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Redis
        var redisConnection = app.Services.GetRequiredService<IRedisConnectionProvider>().Connection;

        await redisConnection.CreateIndexAsync(typeof(LoginSession));
        await redisConnection.CreateIndexAsync(typeof(DeviceOnline));
        await redisConnection.CreateIndexAsync(typeof(DevicePair));
        await redisConnection.CreateIndexAsync(typeof(LcgNode));

        app.UseRateLimiter();
        
        app.UseOpenTelemetryPrometheusScrapingEndpoint(context =>
        {
            if(context.Request.Path != "/metrics") return false;
            
            var remoteIp = context.Connection.RemoteIpAddress;
            return remoteIp is not null && metricsAllowedIpNetworks.Any(x => x.Contains(remoteIp));
        });
        
        app.UseSwagger();

        app.MapScalarApiReference("/scalar/viewer", options => 
                options
                    .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
                    .AddDocument("1", "Version 1")
                    .AddDocument("2", "Version 2")
                );
        
        app.MapControllers();

        return app;
    }

    public static async Task<IApplicationBuilder> ApplyPendingOpenShockMigrations(this IApplicationBuilder app, DatabaseOptions options)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("MigrationHelper");

        logger.LogInformation("Running database migrations...");

        await using var migrationContext = new MigrationOpenShockContext(options.Conn, options.Debug, loggerFactory);

        var pendingMigrations = (await migrationContext.Database.GetPendingMigrationsAsync()).ToArray();

        if (pendingMigrations.Length > 0)
        {
            logger.LogInformation("Found pending migrations, applying [{@Migrations}]", pendingMigrations);
            await migrationContext.Database.MigrateAsync();
            logger.LogInformation("Applied database migrations... proceeding with startup");
        }
        else
        {
            logger.LogInformation("No pending migrations found, proceeding with startup");
        }

        return app;
    }
}