using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenShock.API.Realtime;
using OpenShock.API.Services;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.Email;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.LCGNodeProvisioner;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Services.Turnstile;
using OpenShock.Common.Swagger;
using Scalar.AspNetCore;
using Serilog;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args, options =>
{
    options.ListenAnyIP(80);
#if DEBUG
    options.ListenAnyIP(443, options => options.UseHttps());
#endif
});

builder.RegisterCommonOpenShockOptions();

var databaseConfig = builder.Configuration.GetDatabaseOptions();
var redisConfig = builder.Configuration.GetRedisConfigurationOptions();

builder.Services.AddOpenShockServices(databaseConfig, redisConfig);

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

builder.Services.AddSwaggerExt<Program>();

builder.Services.AddSingleton<ILCGNodeProvisioner, LCGNodeProvisioner>();

// Cloudflare Turnstile
builder.Services.Configure<CloudflareTurnstileOptions>(builder.Configuration.GetRequiredSection(CloudflareTurnstileOptions.Turnstile));
builder.Services.AddSingleton<IValidateOptions<CloudflareTurnstileOptions>, CloudflareTurnstileOptionsValidator>();
builder.Services.AddHttpClient<ICloudflareTurnstileService, CloudflareTurnstileService>();

builder.AddEmailService();

//services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddHostedService<RedisSubscriberService>();

var app = builder.Build();

app.UseCommonOpenShockMiddleware();

if (!databaseConfig.SkipMigration)
{
    Log.Information("Running database migrations...");
    using var scope = app.Services.CreateScope();

    await using var migrationContext = new MigrationOpenShockContext(
        databaseConfig.Conn,
        databaseConfig.Debug,
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
    var pendingMigrations = migrationContext.Database.GetPendingMigrations().ToArray();

    if (pendingMigrations.Length > 0)
    {
        Log.Information("Found pending migrations, applying [{@Migrations}]", pendingMigrations);
        migrationContext.Database.Migrate();
        Log.Information("Applied database migrations... proceeding with startup");
    }
    else
    {
        Log.Information("No pending migrations found, proceeding with startup");
    }
}
else
{
    Log.Warning("Skipping possible database migrations...");
}

app.UseSwaggerExt();

app.MapControllers();

app.MapHub<UserHub>("/1/hubs/user", options => options.Transports = HttpTransportType.WebSockets);
app.MapHub<ShareLinkHub>("/1/hubs/share/link/{id:guid}", options => options.Transports = HttpTransportType.WebSockets);

app.MapScalarApiReference(options => options.OpenApiRoutePattern = "/swagger/{documentName}/swagger.json");

app.Run();
