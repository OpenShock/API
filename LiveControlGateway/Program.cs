using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.DeviceControl;
using OpenShock.Common.Extensions;
using OpenShock.Common.Services;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Swagger;
using OpenShock.LiveControlGateway;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Options;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

builder.RegisterCommonOpenShockOptions();

builder.Services.Configure<LcgOptions>(builder.Configuration.GetRequiredSection(LcgOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<LcgOptions>, LcgOptionsValidator>();

var databaseConfig = builder.Configuration.GetDatabaseOptions();
var redisConfig = builder.Configuration.GetRedisConfigurationOptions();

builder.Services
    .AddOpenShockMemDB(redisConfig)
    .AddOpenShockDB(databaseConfig)
    .AddOpenShockServices()
    .AddOpenShockSignalR(redisConfig);

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IControlSender, ControlSender>();
builder.Services.AddScoped<IOtaService, OtaService>();

builder.AddSwaggerExt<Program>();

//services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddHostedService<LcgKeepAlive>();

builder.Services.AddSingleton<HubLifetimeManager>();

var app = builder.Build();

await app.UseCommonOpenShockMiddleware();

await app.RunAsync();