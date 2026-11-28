using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.AuthenticationHandlers;
using OpenShock.Common.Constants;
using OpenShock.Common.Extensions;
using OpenShock.Common.HealthChecks;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Metrics;
using OpenShock.Common.OpenApi;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Services.AutomationTokens;
using OpenShock.Common.Services.BatchUpdate;
using OpenShock.Common.Services.Configuration;
using OpenShock.Common.Services.Geo;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Services.Webhook;
using OpenShock.Common.Utils;
using OpenShock.Internal.Common.ExceptionHandling;
using OpenTelemetry.Metrics;
using Redis.OM;
using Redis.OM.Contracts;
using StackExchange.Redis;
using JsonOptions = OpenShock.Common.JsonSerialization.JsonOptions;

namespace OpenShock.Common;

public static class OpenShockServiceHelper
{
    /// <summary>
    /// Name of the single OpenAPI document. It must be the framework's default ("v1"): <see cref="OpenApiDocumentPath"/> has no
    /// <c>{documentName}</c> segment, so MapOpenApi serves the default document there. It never appears in a URL.
    /// </summary>
    public const string OpenApiDocumentName = "v1";

    /// <summary>
    /// Path the OpenAPI document is served at.
    /// </summary>
    public const string OpenApiDocumentPath = "/openapi.json";

    /// <summary>
    /// Cap on a single dependency probe. Generous enough not to trip on a slow-but-alive dependency,
    /// short enough that the LCG's 15s keep alive tick is never blocked by a hung one.
    /// </summary>
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long a health report is served before the dependencies are probed again. Matched to the LCG's
    /// keep alive interval, so the gateway still gets a fresh answer on every tick while the far more
    /// frequent container/load balancer probes in between ride along on that same result.
    /// </summary>
    private static readonly TimeSpan HealthCheckCacheTtl = TimeSpan.FromSeconds(15);

    public static IServiceCollection AddOpenShockMemDB(this IServiceCollection services, ConfigurationOptions options)
    {
        // <---- Redis ---->
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options));
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>(serviceProvider =>
            new RedisConnectionProvider(serviceProvider.GetRequiredService<IConnectionMultiplexer>()));
        services.AddSingleton<IRedisPubService, RedisPubService>();

        // Readiness for the store we just registered. IsConnected alone flips late while the
        // multiplexer reconnects, so the check round-trips a PING against the live connection.
        services.AddHealthChecks()
            .AddRedis(
                sp => sp.GetRequiredService<IConnectionMultiplexer>(),
                name: HealthCheckNames.Redis,
                tags: [HealthCheckTags.Ready],
                timeout: HealthCheckTimeout);

        return services;
    }

    public static IServiceCollection AddOpenShockDB(this IServiceCollection services, DatabaseOptions options)
    {
        // <---- Postgres EF Core ---->
        services.AddDbContextPool<OpenShockContext>(builder =>
            OpenShockContext.ConfigureOptionsBuilder(builder, options.Conn, options.Debug));
        services.AddPooledDbContextFactory<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(options.Conn);
            if (options.Debug)
            {
                builder.EnableSensitiveDataLogging();
                builder.EnableDetailedErrors();
            }
        });

        // Readiness for the database we just registered. Deliberately its own connection rather than
        // the pooled EF context: this must answer "is Postgres up", not "is EF's pool warm".
        services.AddHealthChecks()
            .AddNpgSql(
                options.Conn,
                name: HealthCheckNames.Database,
                tags: [HealthCheckTags.Ready],
                timeout: HealthCheckTimeout);

        return services;
    }

    /// <summary>
    /// Made to closely resemble the built-in <see cref="AuthenticationServiceCollectionExtensions.AddAuthentication(IServiceCollection, Action{AuthenticationOptions})"/> implementation.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    private static AuthenticationBuilder AddOpenShockAuthentication(this IServiceCollection services,
        Action<AuthenticationOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddAuthenticationCore();
        services.AddDataProtection().PersistKeysToDbContext<OpenShockContext>();
        // services.AddWebEncoders(); // Exists in original AddAuthentication method
        services.TryAddSingleton(TimeProvider.System);
        // services.TryAddSingleton<ISystemClock, SystemClock>(); // Exists in original AddAuthentication method
        // services.TryAddSingleton<IAuthenticationConfigurationProvider, DefaultAuthenticationConfigurationProvider>(); // Exists in original AddAuthentication method

        var builder = new AuthenticationBuilder(services);

        services.Configure(configureOptions);

        return builder;
    }

    /// <summary>
    /// Configures the OpenAPI document. Each host registers it itself with
    /// <c>services.AddOpenApi(OpenApiDocumentName, ConfigureOpenApi)</c>: the XML documentation comments are applied by a source generator
    /// that only sees the documentation of the project making that call and of the projects it references, so the call belongs in the host.
    /// </summary>
    public static void ConfigureOpenApi(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, context, _) =>
        {
            document.Info.Title = "OpenShock.API";
            document.Info.Version = "1.0";
            document.Info.Description = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;

            var isDevelopment = context.ApplicationServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
            var servers = new List<OpenApiServer>
            {
                new() { Url = "https://api.openshock.app" },
                new() { Url = "https://api.openshock.dev" }
            };
            if (isDevelopment)
            {
                servers.Add(new OpenApiServer { Url = "https://localhost" });
            }
            document.Servers = servers;

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                [OpenShockAuthSchemes.UserSessionCookie] = new OpenApiSecurityScheme
                {
                    Name = AuthConstants.UserSessionCookieName,
                    Description = "Enter user session cookie",
                    In = ParameterLocation.Cookie,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = OpenShockAuthSchemes.UserSessionCookie
                },
                [OpenShockAuthSchemes.ApiToken] = new OpenApiSecurityScheme
                {
                    Name = AuthConstants.ApiTokenHeaderName,
                    Description = "Enter API Token",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = OpenShockAuthSchemes.ApiToken
                },
                [OpenShockAuthSchemes.HubToken] = new OpenApiSecurityScheme
                {
                    Name = AuthConstants.HubTokenHeaderName,
                    Description = "Enter hub token",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = OpenShockAuthSchemes.HubToken
                }
            };

            return Task.CompletedTask;
        });

        options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_2;
        options.CreateSchemaReferenceId = OpenShockSchemaIds.Create;

        options.AddOperationTransformer<OpenShockOperationTransformer>();
        options.AddSchemaTransformer<OpenShockSchemaTransformer>();
        options.AddDocumentTransformer<EnumSchemaTransformer>();

        // Run after the built-in XML comment transformers (registered before this method), filling what they leave out
        options.AddOperationTransformer<DocumentedResponsesTransformer>();
        options.AddDocumentTransformer<ControllerTagDescriptionTransformer>();
    }

    /// <summary>
    /// Register all OpenShock services for PRODUCTION use
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureAuth"></param>
    /// <param name="configureMetrics"></param>
    /// <returns></returns>
    public static IServiceCollection AddOpenShockServices(this IServiceCollection services,
        Action<AuthenticationBuilder>? configureAuth = null, Action<MeterProviderBuilder>? configureMetrics = null)
    {
        // <---- ASP.NET ---->
        // OpenShockExceptionHandler (OpenShock.Internal.AspNet) takes JsonSerializerOptions via ctor injection.
        services.AddSingleton(JsonOptions.Default);
        services.AddExceptionHandler<OpenShockExceptionHandler>();

        services.AddCachedHealthChecks(HealthCheckCacheTtl);

        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024;
            options.MaximumKeyLength = 1024;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });

        var authBuilder = services
            .AddOpenShockAuthentication(opt =>
            {
                opt.DefaultScheme = OpenShockAuthSchemes.UserSessionCookie;
                opt.DefaultAuthenticateScheme = OpenShockAuthSchemes.UserSessionCookie;
            })
            .AddScheme<AuthenticationSchemeOptions, UserSessionAuthentication>(OpenShockAuthSchemes.UserSessionCookie,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, ApiTokenAuthentication>(OpenShockAuthSchemes.ApiToken, _ => { })
            .AddScheme<AuthenticationSchemeOptions, HubAuthentication>(OpenShockAuthSchemes.HubToken, _ => { });

        configureAuth?.Invoke(authBuilder);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(OpenShockAuthPolicies.RankAdmin, policy => policy.RequireRole("Admin", "System"));
            // TODO: Add token permission policies
        });

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, OpenShockAuthorizationMiddlewareResultHandler>();

        services.ConfigureHttpJsonOptions(opt => JsonOptions.ConfigureDefault(opt.SerializerOptions));
        services.AddControllers(options => options.Conventions.Add(new ApiVersionRouteConvention())).AddJsonOptions(opt => JsonOptions.ConfigureDefault(opt.JsonSerializerOptions));

        // Routes carry their version as a plain leading segment (/1/..., /2/...); there is a single document in which a newer
        // version of an endpoint replaces the older one.
        services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, SupersededVersionApiDescriptionProvider>());

        // generic ASP.NET stuff
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddWebEncoders();
        services.AddProblemDetails();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.SetIsOriginAllowed(_ => true);
                builder.AllowAnyHeader();
                builder.AllowCredentials();
                builder.AllowAnyMethod();
                builder.SetPreflightMaxAge(TimeSpan.FromHours(24));
            });
        });

        // This needs to be at this position, earlier will break validation error responses
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblem(context.ModelState);
                return problemDetails.ToObjectResult(context.HttpContext);
            };
        });

        // OpenTelemetry

        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(ControlMetrics.MeterName)
                    .AddMeter("Npgsql") // Database driver: connection pool, command duration and failures
                    .AddPrometheusExporter();

                configureMetrics?.Invoke(metrics);
            });

        // <---- OpenShock Services ---->

        services.AddSingleton<ControlMetrics>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAutomationTokenService, AutomationTokenService>();

        // Ensure GeoOptions is always resolvable so IpEnrichmentService can activate even in hosts
        // (Cron, SeedE2E) that don't call RegisterGeoOptions(). TryAdd leaves the
        // API's config-bound instance untouched; other hosts get a disabled default (no DB paths).
        services.TryAddSingleton(new GeoOptions());
        services.AddSingleton<IIpEnrichmentService, IpEnrichmentService>();
        services.AddHttpClient<IWebhookService, WebhookService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IBatchUpdateService, BatchUpdateService>();
        services.AddHostedService<BatchUpdateService>(provider =>
            (BatchUpdateService)provider.GetRequiredService<IBatchUpdateService>());

        // <---- Rate Limiter Setup ---->

        // Disable rate limiting when:
        // - ASPNETCORE_UNDER_INTEGRATION_TEST=1 (.NET WebApplicationFactory tests, in-process)
        // - OPENSHOCK_DISABLE_RATE_LIMITING=1  (external Playwright tests against a real server)
        var underIntegrationTest =
            Environment.IsFlagSet("ASPNETCORE_UNDER_INTEGRATION_TEST") ||
            Environment.IsFlagSet("OPENSHOCK_DISABLE_RATE_LIMITING");

        services.AddRateLimiter(options =>
        {
            if (underIntegrationTest)
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("test-nolimit"));
                options.AddPolicy("auth", _ => RateLimitPartition.GetNoLimiter("test-nolimit"));
                options.AddPolicy("token-reporting", _ => RateLimitPartition.GetNoLimiter("test-nolimit"));
                options.AddPolicy("shocker-logs", _ => RateLimitPartition.GetNoLimiter("test-nolimit"));
                options.AddPolicy("control-stats", _ => RateLimitPartition.GetNoLimiter("test-nolimit"));
                return;
            }

            // If the request resolved an automation token granting RateLimit, skip every limiter on it.
            // Selectors are sync and the AutomationTokenMiddleware (which runs before UseRateLimiter)
            // has already populated HttpContext.Items, so this is just a dictionary lookup.
            static RateLimitPartition<string>? TryBypass(HttpContext ctx)
            {
                var bypass = ctx.GetResolvedAutomationToken();
                return bypass is not null && bypass.Types.Contains(AutomationTokenType.RateLimit)
                    ? RateLimitPartition.GetNoLimiter($"automation-token-{bypass.Id}")
                    : null;
            }

            options.OnRejected = async (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting");

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (!context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    retryAfter = TimeSpan.FromMinutes(1);

                var retryAfterSeconds = Math.Ceiling(retryAfter.TotalSeconds).ToString("R", CultureInfo.InvariantCulture);

                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds;

                logger.LogWarning("Rate limit hit. IP: {IP}, Path: {Path}, User: {User}, Retry-After: {RetryAfter}s",
                    context.HttpContext.Connection.RemoteIpAddress,
                    context.HttpContext.Request.Path,
                    context.HttpContext.User.Identity?.Name ?? "Anonymous",
                    retryAfterSeconds);

                await context.HttpContext.Response.WriteAsync("Too Many Requests. Please try again later.",
                    cancellationToken);
            };

            // Global fallback limiter
            // Fixed window at 10k requests allows 20k bursts if burst occurs at window boundary
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (TryBypass(context) is { } bypassPartition) return bypassPartition;

                var user = context.User;
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    var ip = context.GetRemoteIP();
                    if (IPAddress.IsLoopback(ip)) return RateLimitPartition.GetNoLimiter("ip-loopback-nolimit");

                    return RateLimitPartition.GetSlidingWindowLimiter($"ip-{ip}", _ =>
                        new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 1000,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 100
                        });
                }

                if (user.HasClaim(claim => claim is { Type: ClaimTypes.Role, Value: "Admin" or "System" }))
                    return RateLimitPartition.GetNoLimiter("privileged-nolimit");

                return RateLimitPartition.GetSlidingWindowLimiter($"user-{userId}", _ =>
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    });
            });

            // Authentication endpoints limiter
            options.AddPolicy("auth", context =>
            {
                if (TryBypass(context) is { } bypassPartition) return bypassPartition;

                var ip = context.GetRemoteIP().ToString();
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                });
            });

            // Token reporting endpoint concurrency limiter
            options.AddPolicy("token-reporting", context =>
                TryBypass(context) ??
                RateLimitPartition.GetConcurrencyLimiter("token-reporting", _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                }));

            // Log fetching endpoint concurrency limiter
            options.AddPolicy("shocker-logs", context =>
                TryBypass(context) ??
                RateLimitPartition.GetConcurrencyLimiter("shocker-logs", _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 10,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                }));

            // Control stats aggregate a user's whole history, so cap how many run at once
            options.AddPolicy("control-stats", context =>
                TryBypass(context) ??
                RateLimitPartition.GetConcurrencyLimiter("control-stats", _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 10,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                }));
        });

        return services;
    }

    public static IServiceCollection AddOpenShockSignalR(this IServiceCollection services,
        ConfigurationOptions redisConfig)
    {
        services.AddOptions<OpenShock.Common.Hubs.HubRevalidationOptions>();
        services.AddSingleton<OpenShock.Common.Hubs.HubConnectionTracker>();
        services.AddSingleton<OpenShock.Common.Hubs.RevalidateAuthenticationHubFilter>();

        services.AddSignalR(options => Microsoft.AspNetCore.SignalR.HubOptionsExtensions.AddFilter<OpenShock.Common.Hubs.RevalidateAuthenticationHubFilter>(options))
            .AddOpenShockStackExchangeRedis(options => { options.Configuration = redisConfig; })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                options.PayloadSerializerOptions.Converters.Add(new SemVersionJsonConverter());
            });

        return services;
    }
}
