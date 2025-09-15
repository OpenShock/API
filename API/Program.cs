using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Connections;
using OpenShock.API.OAuth;
using OpenShock.API.Options.OAuth;
using OpenShock.API.Realtime;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.DeviceUpdate;
using OpenShock.API.Services.Email;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.API.Services.Turnstile;
using OpenShock.API.Services.UserService;
using OpenShock.Common;
using OpenShock.Common.DeviceControl;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.Services;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.LCGNodeProvisioner;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Swagger;
using Serilog;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

var redisOptions = builder.RegisterRedisOptions();
var databaseOptions = builder.RegisterDatabaseOptions();
builder.RegisterMetricsOptions();
builder.RegisterFrontendOptions();

builder.Services
    .AddOpenShockMemDB(redisOptions)
    .AddOpenShockDB(databaseOptions)
    .AddOpenShockServices(auth =>
    {
        auth.AddCookie(OAuthConstants.FlowScheme, o => {
            o.Cookie.Name = OAuthConstants.FlowCookieName;
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = false;
        });
            
        var options = builder.Configuration.GetSection(DiscordOAuthOptions.SectionName).Get<DiscordOAuthOptions>();
        if (options is not null)
        {
            auth.AddDiscord(OAuthConstants.DiscordScheme, o => {
                o.SignInScheme = OAuthConstants.FlowScheme;

            

                o.ClientId = options.ClientId;
                o.ClientSecret = options.ClientSecret;
                o.CallbackPath = "/oauth/discord/callback";
                o.AccessDeniedPath = "/oauth/discord/rejected"; // TODO: Make this do something
                o.Scope.Add("email");

                o.Prompt = "none";
                o.SaveTokens = false;

                o.ClaimActions.MapJsonKey(OAuthConstants.ClaimEmailVerified, "verified");
                o.ClaimActions.MapJsonKey(OAuthConstants.ClaimDisplayName, "global_name");

                o.Validate();
            });
        }
    })
    .AddOpenShockSignalR(redisOptions);

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IControlSender, ControlSender>();
builder.Services.AddScoped<IOtaService, OtaService>();
builder.Services.AddScoped<IDeviceUpdateService, DeviceUpdateService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IOAuthConnectionService, OAuthConnectionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILCGNodeProvisioner, LCGNodeProvisioner>();

builder.AddSwaggerExt<Program>();

builder.AddCloudflareTurnstileService();
builder.AddEmailService();

//services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddHostedService<RedisSubscriberService>();

var app = builder.Build();

await app.UseCommonOpenShockMiddleware();

if (!databaseOptions.SkipMigration)
{
    await app.ApplyPendingOpenShockMigrations(databaseOptions);
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