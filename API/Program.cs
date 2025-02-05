using System.Configuration;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using OpenShock.API;
using OpenShock.API.Realtime;
using OpenShock.API.Services;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.Email.Mailjet;
using OpenShock.API.Services.Email.Smtp;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.OpenShockDb;
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

var config = builder.GetAndRegisterOpenShockConfig<ApiConfig>();
var commonServices = builder.Services.AddOpenShockServices(config);

builder.Services.AddSignalR()
    .AddOpenShockStackExchangeRedis(options => { options.Configuration = commonServices.RedisConfig; })
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

builder.Services.AddSingleton(x =>
{
    if (config.Turnstile.Enabled && (string.IsNullOrWhiteSpace(config.Turnstile.SecretKey) || string.IsNullOrWhiteSpace(config.Turnstile.SecretKey)))
    {
        throw new ConfigurationErrorsException("Turnstile is enabled in config, but secretkey and/or token is missing or empty");
    }
    
    return new CloudflareTurnstileOptions
    {
        Enabled = config.Turnstile.Enabled,
        SecretKey = config.Turnstile.SecretKey ?? string.Empty,
        SiteKey = config.Turnstile.SiteKey ?? string.Empty
    };
});
builder.Services.AddHttpClient<ICloudflareTurnstileService, CloudflareTurnstileService>();

// ----------------- MAIL SETUP -----------------
var emailConfig = config.Mail;
switch (emailConfig.Type)
{
    case ApiConfig.MailConfig.MailType.Mailjet:
        if (emailConfig.Mailjet == null)
            throw new Exception("Mailjet config is null but mailjet is selected as mail type");
        builder.Services.AddMailjetEmailService(emailConfig.Mailjet, emailConfig.Sender);
        break;
    case ApiConfig.MailConfig.MailType.Smtp:
        if (emailConfig.Smtp == null)
            throw new Exception("SMTP config is null but SMTP is selected as mail type");
        builder.Services.AddSmtpEmailService(emailConfig.Smtp, emailConfig.Sender, new SmtpServiceTemplates
        {
            PasswordReset = SmtpTemplate.ParseFromFileThrow("SmtpTemplates/PasswordReset.liquid").Result,
            EmailVerification = SmtpTemplate.ParseFromFileThrow("SmtpTemplates/EmailVerification.liquid").Result
        });
        break;
    default:
        throw new Exception("Unknown mail type");
}

//services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddHostedService<RedisSubscriberService>();

var app = builder.Build();

app.UseCommonOpenShockMiddleware();

if (!config.Db.SkipMigration)
{
    Log.Information("Running database migrations...");
    using var scope = app.Services.CreateScope();
    
    await using var migrationContext = new MigrationOpenShockContext(
        config.Db.Conn,
        config.Db.Debug, 
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
