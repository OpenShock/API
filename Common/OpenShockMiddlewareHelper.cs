using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenShock.Common.HealthChecks;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using IPNetwork = System.Net.IPNetwork;

using OpenShock.Internal.Common.Utils;

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
        // Networks allowed to reach the operational endpoints (metrics, health). Defaults to the
        // private ranges, so in-cluster scrapers and container probes work without configuration.
        var metricsOptions = app.Services.GetRequiredService<MetricsOptions>();
        var internalAllowedIpNetworks = metricsOptions.AllowedNetworks.Select(IPNetwork.Parse).ToArray();

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

        // Health is operational data, gated by the same allow-list as /metrics. This runs after
        // UseForwardedHeaders, so a proxied request is judged on its real client IP, and before routing,
        // so a rejected caller cannot even tell the endpoints exist.
        app.Use(async (HttpContext context, RequestDelegate next) =>
        {
            if (context.Request.Path.StartsWithSegments(HealthCheckPaths.Health)
                && !IsAllowed(context.Connection.RemoteIpAddress, internalAllowedIpNetworks))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next(context);
        });

        app.UseSerilogRequestLogging(options =>
        {
            // Probes hit the health endpoint every few seconds and would drown the request log, so drop
            // *successful* ones to Verbose. A failing probe is the thing we actually want to see, so it
            // - and every other request - keeps Serilog's own level policy instead of a hand-rolled copy.
            var defaultGetLevel = options.GetLevel;
            options.GetLevel = (context, elapsed, exception) =>
                exception is null
                && context.Response.StatusCode < 400
                && context.Request.Path.StartsWithSegments(HealthCheckPaths.Health)
                    ? LogEventLevel.Verbose
                    : defaultGetLevel(context, elapsed, exception);
        });

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

            return IsAllowed(context.Connection.RemoteIpAddress, internalAllowedIpNetworks);
        });
        
        app.UseSwagger();

        app.MapScalarApiReference("/scalar/viewer", options => 
                options
                    .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
                    .AddDocument("1", "Version 1")
                    .AddDocument("2", "Version 2")
                );
        
        // Reports whether every dependency this host needs to serve is reachable. The LCG gates its
        // self-onlining on the same set, so a probe sees exactly what the gateway acts on.
        app.MapHealthChecks(HealthCheckPaths.Health, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
            ResponseWriter = WriteHealthReportAsync
        });

        app.MapControllers();

        return app;
    }

    private static bool IsAllowed(System.Net.IPAddress? remoteIp, IPNetwork[] allowedNetworks) =>
        remoteIp is not null && allowedNetworks.Any(network => network.Contains(remoteIp));

    /// <summary>
    /// Writes the per-check breakdown instead of the default bare status word, so a failing probe says
    /// which dependency is down. Safe to be this detailed: the endpoint is allow-list gated.
    /// </summary>
    private static Task WriteHealthReportAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(HealthReportResponse.From(report));
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

            // A migration may have introduced new Postgres types (e.g. a new enum, or a new value on an
            // existing one). Npgsql snapshots the type catalog (OID -> type) on the runtime data source's
            // first connection and caches it for the process lifetime. Since this pooled data source is
            // distinct from the throwaway migration context above, it can hold a catalog from before the
            // migration ran and fail reading the new type with "DataTypeName '-.-'". Reload it now that the
            // new types exist so this process does not need a restart to see them.
            await ReloadRuntimeTypeCatalogAsync(scope.ServiceProvider, logger);
        }
        else
        {
            logger.LogInformation("No pending migrations found, proceeding with startup");
        }

        return app;
    }

    /// <summary>
    /// Forces the runtime pooled <see cref="OpenShockContext"/>'s Npgsql data source to reload its Postgres
    /// type catalog. Called after migrations are applied so newly-added types (enums) are visible without a
    /// process restart. Best-effort: a failure here is logged but not fatal, since a restart also clears it.
    /// </summary>
    private static async Task ReloadRuntimeTypeCatalogAsync(IServiceProvider services, Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            await using var context = services.GetRequiredService<OpenShockContext>();
            // The connection is owned by the DbContext and disposed with it; do not dispose it here.
#pragma warning disable IDISP001
            var connection = (Npgsql.NpgsqlConnection)context.Database.GetDbConnection();
#pragma warning restore IDISP001
            await connection.OpenAsync();
            await connection.ReloadTypesAsync();
            logger.LogInformation("Reloaded Npgsql type catalog after applying migrations");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to reload Npgsql type catalog after migrations; if a migration added a new Postgres " +
                "type, a restart of this process may be required for it to be readable");
        }
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

        // If migrations are skipped (schema is managed out-of-band), there is no migrator to wait for -
        // blocking here would just stall startup until the timeout. Skip the gate, same as the API skips
        // applying migrations under this flag.
        if (options.SkipMigration)
        {
            logger.LogWarning("SkipMigration is set; not waiting for database schema readiness");
            return app;
        }

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