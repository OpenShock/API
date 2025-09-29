using System.Net;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.AuthenticationHandlers;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.ExceptionHandle;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.BatchUpdate;
using OpenShock.Common.Services.Configuration;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Services.Webhook;
using OpenTelemetry.Metrics;
using Redis.OM;
using Redis.OM.Contracts;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using OpenShock.Common.Extensions;
using OpenShock.Common.Utils;
using JsonOptions = OpenShock.Common.JsonSerialization.JsonOptions;

namespace OpenShock.Common;

public static class OpenShockServiceHelper
{
    public static IServiceCollection AddOpenShockMemDB(this IServiceCollection services, ConfigurationOptions options)
    {
        // <---- Redis ---->
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options));
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>(serviceProvider => new RedisConnectionProvider(serviceProvider.GetRequiredService<IConnectionMultiplexer>()));
        services.AddSingleton<IRedisPubService, RedisPubService>();

        return services;
    }

    public static IServiceCollection AddOpenShockDB(this IServiceCollection services, DatabaseOptions options)
    {
        // <---- Postgres EF Core ---->
        services.AddDbContextPool<OpenShockContext>(builder => OpenShockContext.ConfigureOptionsBuilder(builder, options.Conn, options.Debug));
        services.AddPooledDbContextFactory<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(options.Conn);
            if (options.Debug)
            {
                builder.EnableSensitiveDataLogging();
                builder.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>
    /// Made to closely resemble the built-in <see cref="AuthenticationServiceCollectionExtensions.AddAuthentication(IServiceCollection, Action{AuthenticationOptions})"/> implementation.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    private static AuthenticationBuilder AddOpenShockAuthentication(this IServiceCollection services, Action<AuthenticationOptions> configureOptions)
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
    /// Register all OpenShock services for PRODUCTION use
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureAuth"></param>
    /// <returns></returns>
    public static IServiceCollection AddOpenShockServices(this IServiceCollection services, Action<AuthenticationBuilder>? configureAuth = null)
    {
        // <---- ASP.NET ---->
        services.AddExceptionHandler<OpenShockExceptionHandler>();

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

        services.AddScoped<IUserReferenceService, UserReferenceService>();

        var authBuilder = services
            .AddOpenShockAuthentication(opt =>
            {
                opt.DefaultScheme = OpenShockAuthSchemes.UserSessionCookie;
                opt.DefaultAuthenticateScheme = OpenShockAuthSchemes.UserSessionCookie;
            })
            .AddScheme<AuthenticationSchemeOptions, UserSessionAuthentication>(OpenShockAuthSchemes.UserSessionCookie, _ => { })
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
        services.AddControllers().AddJsonOptions(opt => JsonOptions.ConfigureDefault(opt.JsonSerializerOptions));
        
        var apiVersioningBuilder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
        });

        apiVersioningBuilder.AddApiExplorer(setup =>
        {
            setup.GroupNameFormat = "VVV";
            setup.SubstituteApiVersionInUrl = true;
            setup.DefaultApiVersion = new ApiVersion(1, 0);
            setup.AssumeDefaultVersionWhenUnspecified = true;
        });
        
        // generic ASP.NET stuff
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddWebEncoders();
        services.AddProblemDetails();
        
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.SetIsOriginAllowed(s => true);
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
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddPrometheusExporter());
        
        // <---- OpenShock Services ---->

        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddHttpClient<IWebhookService, WebhookService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IBatchUpdateService, BatchUpdateService>();
        services.AddHostedService<BatchUpdateService>(provider =>
            (BatchUpdateService)provider.GetRequiredService<IBatchUpdateService>());

        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting");

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (!context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    retryAfter = TimeSpan.FromMinutes(1);

                var retryAfterSeconds = Math.Ceiling(retryAfter.TotalSeconds).ToString("R");

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
                var user = context.User;
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    var ip = context.GetRemoteIP();
                    if (IPAddress.IsLoopback(ip)) return RateLimitPartition.GetNoLimiter("ip-loopback-nolimit");

                    return RateLimitPartition.GetSlidingWindowLimiter($"ip-{ip}", _ => new SlidingWindowRateLimiterOptions
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
                var ip = context.GetRemoteIP();
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                });
            });

            // Token reporting endpoint concurrency limiter
            options.AddPolicy("token-reporting", _ =>
                RateLimitPartition.GetConcurrencyLimiter("token-reporting", _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                }));

            // Log fetching endpoint concurrency limiter
            options.AddPolicy("shocker-logs", _ =>
                RateLimitPartition.GetConcurrencyLimiter("shocker-logs", _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 10,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                }));
        });

        return services;
    }

    public static IServiceCollection AddOpenShockSignalR(this IServiceCollection services, ConfigurationOptions redisConfig)
    {
        services.AddSignalR()
            .AddOpenShockStackExchangeRedis(options => { options.Configuration = redisConfig; })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                options.PayloadSerializerOptions.Converters.Add(new SemVersionJsonConverter());
            });
        
        return services;
    }
}