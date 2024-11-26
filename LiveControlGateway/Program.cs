using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.PubSub;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddJsonFile("appsettings.Custom.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

var isDevelopment = builder.Environment.IsDevelopment();
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = isDevelopment;
    options.ValidateOnBuild = isDevelopment;
});
builder.WebHost.ConfigureKestrel(serverOptions =>
{
#if DEBUG
    serverOptions.ListenAnyIP(580);
    serverOptions.ListenAnyIP(5443, options => options.UseHttps("devcert.pfx"));
#else
    serverOptions.ListenAnyIP(80);
#endif
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMilliseconds(3000);
});

builder.Services.AddSerilog();

var config = builder.GetAndRegisterOpenShockConfig<LCGConfig>();
var commonService = builder.Services.AddOpenShockServices(config);

builder.Services.AddSignalR()
    .AddOpenShockStackExchangeRedis(options => { options.Configuration = commonService.RedisConfig; })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
        options.PayloadSerializerOptions.Converters.Add(new SemVersionJsonConverter());
    });

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IOtaService, OtaService>();

builder.Services.AddSwaggerExt("OpenShock.LiveControlGateway");

builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
//services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddHostedService<RedisSubscriberService>();
builder.Services.AddHostedService<LcgKeepAlive>();

builder.Services.AddSingleton<HubLifetimeManager>();

var app = builder.Build();

app.UseCommonOpenShockMiddleware();

app.UseSwaggerExt();

app.MapControllers();

app.Run();