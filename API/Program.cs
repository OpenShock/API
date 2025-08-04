using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Options;
using OpenShock.API.Realtime;
using OpenShock.API.Services;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.Email;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.LCGNodeProvisioner;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Services.Turnstile;
using OpenShock.Common.Swagger;
using OpenShock.Common.Utils;
using Serilog;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

#region Config

builder.RegisterCommonOpenShockOptions();

builder.Services.Configure<FrontendOptions>(builder.Configuration.GetRequiredSection(FrontendOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<FrontendOptions>, FrontendOptionsValidator>();

var databaseConfig = builder.Configuration.GetDatabaseOptions();
var redisConfig = builder.Configuration.GetRedisConfigurationOptions();

#endregion

builder.Services.AddOpenShockMemDB(redisConfig);
builder.Services.AddOpenShockDB(databaseConfig);
builder.Services.AddOpenShockServices();

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting");

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (!context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfter = TimeSpan.FromMinutes(1);
        }

        var retryAfterSeconds = Math.Ceiling(retryAfter.TotalSeconds).ToString("F0");

        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds;

        logger.LogWarning("Rate limit hit. IP: {IP}, Path: {Path}, User: {User}, Retry-After: {RetryAfter}s",
            context.HttpContext.Connection.RemoteIpAddress,
            context.HttpContext.Request.Path,
            context.HttpContext.User.Identity?.Name ?? "Anonymous",
            retryAfterSeconds);

        await context.HttpContext.Response.WriteAsync("Too Many Requests. Please try again later.", cancellationToken);
    };

    // Global fallback limiter
    // Fixed window at 10k requests allows 20k bursts if burst occurs at window boundry
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10000,
            Window = TimeSpan.FromMinutes(1)
        }));

    // Per-IP limiter
    options.AddPolicy("per-ip", context =>
    {
        var ip = context.GetRemoteIP();
        if (IPAddress.IsLoopback(ip))
        {
            return RateLimitPartition.GetNoLimiter(IPAddress.Loopback);
        }

        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 1000,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // Per-user limiter
    options.AddPolicy("per-user", context =>
    {
        var user = context.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

        if (user.HasClaim(claim => claim is { Type: ClaimTypes.Role, Value: "Admin" or "System" }))
        {
            return RateLimitPartition.GetNoLimiter($"user-{userId}-privileged");
        }

        return RateLimitPartition.GetSlidingWindowLimiter($"user-{userId}", _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 600,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
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

builder.Services.AddSignalR()
    .AddOpenShockStackExchangeRedis(options => { options.Configuration = redisConfig; })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
        options.PayloadSerializerOptions.Converters.Add(new SemVersionJsonConverter());
    });

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IOtaService, OtaService>();
builder.Services.AddScoped<IDeviceUpdateService, DeviceUpdateService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ILCGNodeProvisioner, LCGNodeProvisioner>();

builder.AddSwaggerExt<Program>();

builder.AddCloudflareTurnstileService();
builder.AddEmailService();

//services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddHostedService<RedisSubscriberService>();

var app = builder.Build();

await app.UseCommonOpenShockMiddleware();

if (!databaseConfig.SkipMigration)
{
    await app.ApplyPendingOpenShockMigrations(databaseConfig);
}
else
{
    Log.Warning("Skipping possible database migrations...");
}

app.MapHub<UserHub>("/1/hubs/user", options => options.Transports = HttpTransportType.WebSockets);
app.MapHub<PublicShareHub>("/1/hubs/share/link/{id:guid}", options => options.Transports = HttpTransportType.WebSockets);

await app.RunAsync();

// Expose Program class for integrationtests
public partial class Program;