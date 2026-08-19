using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Connections;
using OpenShock.API.Options.OAuth;
using OpenShock.API.Realtime;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.DeviceUpdate;
using OpenShock.API.Services.LCGNodeProvisioner;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.API.Services.Token;
using OpenShock.API.Services.Turnstile;
using OpenShock.API.Services.UserService;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.Services;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Swagger;
using Serilog;
using OAuthConstants = OpenShock.API.OAuth.OAuthConstants;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

var redisOptions = builder.RegisterRedisOptions();
var databaseOptions = builder.RegisterDatabaseOptions();
builder.RegisterMetricsOptions();
builder.RegisterFrontendOptions();
builder.RegisterGeoOptions();
builder.RegisterAccountOptions();
// The API never sends mail, but it must know whether anything ever will: with mail disabled there is
// no activation link, so accounts are activated on creation instead of waiting for one.
builder.RegisterMailOptions();

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
                options.ClientId = twitterOptions.ClientId;
                options.ClientSecret = twitterOptions.ClientSecret;

                // The package default of tweet.read + users.read covers id/username/name. tweet.read looks
                // redundant for a login, but X's spec requires both for /2/users/me, so it stays.
                // Email needs its own scope and field, and the X app must have "Request email from users"
                // enabled in the developer dashboard - without that the field is silently absent.
                options.Scope.Add("users.email");
                options.UserFields.Add("confirmed_email");

                // The user info payload is wrapped in a "data" object, so these need custom resolvers.
                options.ClaimActions.MapCustomJson(OAuthConstants.ClaimDisplayName, user => GetUserField(user, "name"));
                options.ClaimActions.MapCustomJson(ClaimTypes.Email, user => GetUserField(user, "confirmed_email"));
                // X only ever returns an address it has confirmed itself, so its presence is the verification signal.
                options.ClaimActions.MapCustomJson(OAuthConstants.ClaimEmailVerified, user =>
                    GetUserField(user, "confirmed_email") is null ? null : "true");

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

        static string? GetUserField(JsonElement user, string field) =>
            user.TryGetProperty("data", out var data) && data.TryGetProperty(field, out var value)
                ? value.GetString()
                : null;
    })
    .AddOpenShockSignalR(redisOptions);

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IControlSender, ControlSender>();
builder.Services.AddScoped<IOtaService, OtaService>();
builder.Services.AddScoped<IDeviceUpdateService, DeviceUpdateService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IOAuthConnectionService, OAuthConnectionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IApiTokenService, ApiTokenService>();
builder.Services.AddScoped<ILCGNodeProvisioner, LCGNodeProvisioner>();

builder.AddSwaggerExt<Program>();

builder.AddCloudflareTurnstileService();

builder.Services.AddHostedService<RedisSubscriberService>();

var app = builder.Build();

// Optional path prefix the API is served under (e.g. "/api" when reverse-proxied on a
// shared host). Empty by default -> served at root, unchanged behavior.
var apiPathBase = builder.Configuration.GetValue<string>("OpenShock:Api:PathBase");

await app.UseCommonOpenShockMiddleware(apiPathBase);

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