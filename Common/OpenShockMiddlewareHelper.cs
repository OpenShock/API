using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Scalar.AspNetCore;
using Serilog;
using IPNetwork = System.Net.IPNetwork;

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
    
    public static async Task<IApplicationBuilder> UseCommonOpenShockMiddleware(this WebApplication app, string? pathBase = null)
    {
        var metricsOptions = app.Services.GetRequiredService<MetricsOptions>();
        var metricsAllowedIpNetworks = metricsOptions.AllowedNetworks.Select(IPNetwork.Parse).ToArray();

        // Mount the whole app under a path prefix (e.g. "/api" or "/gateway") when configured.
        // This must run before routing so controllers still match at their root-relative routes,
        // and it records Request.PathBase so generated URLs (OAuth redirect_uri, Scalar, ...)
        // include the prefix. The proxy forwards the full path unchanged (no StripPrefix).
        if (!string.IsNullOrWhiteSpace(pathBase))
        {
            app.UsePathBase(NormalizePathBase(pathBase));
        }

        foreach (var proxy in await TrustedProxiesFetcher.GetTrustedNetworksAsync())
        {
            ForwardedSettings.KnownIPNetworks.Add(proxy);
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

    /// <summary>
    /// Normalizes a configured path base into a valid <see cref="PathString"/>: ensures a single
    /// leading slash and strips any trailing slash (so "api", "/api", "/api/" all become "/api").
    /// </summary>
    private static string NormalizePathBase(string pathBase)
    {
        var trimmed = pathBase.Trim().Trim('/');
        return trimmed.Length == 0 ? "/" : "/" + trimmed;
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
            logger.LogInformation("Found pending migrations, applying [{@Migrations}]", string.Join(", ", pendingMigrations));
            await migrationContext.Database.MigrateAsync();
            logger.LogInformation("Applied database migrations... proceeding with startup");
        }
        else
        {
            logger.LogInformation("No pending migrations found, proceeding with startup");
        }

        return app;
    }

    /// <summary>
    /// Blocks startup until the database schema is up to date (no pending migrations), for hosts that do
    /// <em>not</em> own migrations (e.g. the Cron worker - the API is the sole migrator). This is not just
    /// tidiness: <see cref="OpenShockContext"/> binds Postgres enum types (<c>email_status</c>,
    /// <c>email_type</c>, ...) to CLR enums <em>by name</em>, and Npgsql resolves those names against the
    /// type catalog it loads on the data source's first connection and caches for the life of the process.
    /// If that first connection happens before the migrator has created a newly-added enum, every query
    /// using it fails permanently ("data type name '...' could not be found") until the process restarts.
    /// Waiting here guarantees the schema - and its enum types - exists before anything opens the pooled
    /// context, closing the deploy-time race without this host performing any schema writes of its own.
    /// </summary>
    public static async Task<IApplicationBuilder> WaitForOpenShockSchemaReady(this IApplicationBuilder app,
        DatabaseOptions options, TimeSpan? timeout = null)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SchemaReadyGate");

        logger.LogInformation("Waiting for database schema to be up to date (migrations owned by another host)...");

        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromMinutes(5));
        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(15);

        while (true)
        {
            try
            {
                await using var migrationContext = new MigrationOpenShockContext(options.Conn, options.Debug, loggerFactory);
                var pending = (await migrationContext.Database.GetPendingMigrationsAsync()).ToArray();
                if (pending.Length == 0)
                {
                    logger.LogInformation("Database schema is up to date; proceeding with startup");
                    return app;
                }

                logger.LogWarning("Schema not ready: {Count} pending migration(s) [{Migrations}] not yet applied by the migrator",
                    pending.Length, string.Join(", ", pending));
            }
            catch (Exception ex)
            {
                // Database not reachable yet, or the migrator is mid-run. Keep waiting rather than crash-looping.
                logger.LogWarning(ex, "Could not determine migration state; will retry");
            }

            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException(
                    "Timed out waiting for the database schema to be brought up to date by the migrator. " +
                    "Ensure the API host (the sole migrator) is running and reachable.");

            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(Math.Min(maxDelay.TotalSeconds, delay.TotalSeconds * 2));
        }
    }
}