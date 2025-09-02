using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Options;
using OpenShock.API.Realtime;
using OpenShock.API.Services;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.Email;
using OpenShock.API.Services.UserService;
using OpenShock.Common;
using OpenShock.Common.DeviceControl;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Options;
using OpenShock.Common.Services;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.LCGNodeProvisioner;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Services.Turnstile;
using OpenShock.Common.Swagger;
using Serilog;
using OpenShock.API.Services.OAuth;
using OpenShock.API.Services.OAuth.Discord;
using DiscordOAuthOptions = OpenShock.API.Options.OAuth.DiscordOAuthOptions;

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

builder.Services.AddSignalR()
    .AddOpenShockStackExchangeRedis(options => { options.Configuration = redisConfig; })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
        options.PayloadSerializerOptions.Converters.Add(new SemVersionJsonConverter());
    });

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IControlSender, ControlSender>();
builder.Services.AddScoped<IOtaService, OtaService>();
builder.Services.AddScoped<IDeviceUpdateService, DeviceUpdateService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILCGNodeProvisioner, LCGNodeProvisioner>();

builder.Services.AddSingleton<IOAuthStateStore, CookieOAuthStateStore>();
builder.Services.Configure<DiscordOAuthOptions>(builder.Configuration.GetRequiredSection(DiscordOAuthOptions.SectionName));
builder.Services.AddSingleton<IOAuthHandler, DiscordOAuthHandler>();
builder.Services.AddSingleton<IOAuthHandlerRegistry, OAuthHandlerRegistry>();

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