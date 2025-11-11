using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Connections;
using OpenShock.API.Options.OAuth;
using OpenShock.API.Realtime;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.DeviceUpdate;
using OpenShock.API.Services.Email;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.API.Services.Turnstile;
using OpenShock.API.Services.UserService;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.OpenAPI;
using OpenShock.Common.Services;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.LCGNodeProvisioner;
using OpenShock.Common.Services.Ota;
using Serilog;
using OAuthConstants = OpenShock.API.OAuth.OAuthConstants;

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
            
        var discordOptions = builder.Configuration.GetSection(DiscordOAuthOptions.SectionName).Get<DiscordOAuthOptions>();
        if (discordOptions is not null)
        {
            auth.AddDiscord(OAuthConstants.DiscordScheme, options => {
                DefaultOptions(options, "discord");
                options.ClientId = discordOptions.ClientId;
                options.ClientSecret = discordOptions.ClientSecret;
                
                options.Scope.Add("email");
                
                options.Prompt = "none";

                options.ClaimActions.MapJsonKey(OAuthConstants.ClaimEmailVerified, "verified");
                options.ClaimActions.MapJsonKey(OAuthConstants.ClaimDisplayName, "global_name");

                options.Validate();
            });
        }
        
        var googleOptions = builder.Configuration.GetSection(GoogleOAuthOptions.SectionName).Get<GoogleOAuthOptions>();
        if (googleOptions is not null)
        {
            auth.AddGoogle(OAuthConstants.GoogleScheme, options => {
                DefaultOptions(options, "google");
                options.ClientId = googleOptions.ClientId;
                options.ClientSecret = googleOptions.ClientSecret;
                
                options.Validate();
            });
        }
        
        var twitterOptions = builder.Configuration.GetSection(TwitterOAuthOptions.SectionName).Get<TwitterOAuthOptions>();
        if (twitterOptions is not null)
        {
            auth.AddTwitter(OAuthConstants.TwitterScheme, options => {
                DefaultOptions(options, "twitter");
                options.ConsumerKey = twitterOptions.ConsumerKey;
                options.ConsumerSecret = twitterOptions.ConsumerSecret;

                options.Validate();
            });
        }

        return;

        static void DefaultOptions(RemoteAuthenticationOptions options, string provider)
        {
            options.SignInScheme = OAuthConstants.FlowScheme;
            
            options.CallbackPath = $"/oauth/{provider}/callback";
            options.AccessDeniedPath = $"/oauth/{provider}/rejected"; // TODO: Make this do something
                
            options.SaveTokens = false;
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

builder.AddOpenApiExt<Program>();

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