using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.Services;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Swagger;
using OpenShock.LiveControlGateway;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Options;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

var redisOptions = builder.RegisterRedisOptions();
var databaseOptions = builder.RegisterDatabaseOptions();
builder.RegisterMetricsOptions();

var lcgOptions = builder.Configuration.GetRequiredSection(LcgOptions.SectionName).Get<LcgOptions>();
if (lcgOptions is null)
    throw new InvalidOperationException($"Missing or invalid configuration for {LcgOptions.SectionName}.");

builder.Services.AddSingleton<IValidateOptions<LcgOptions>, LcgOptionsValidator>();
builder.Services.AddSingleton(lcgOptions);

builder.Services
    .AddOpenShockMemDB(redisOptions)
    .AddOpenShockDB(databaseOptions)
    .AddOpenShockServices(configureMetrics: metricsBuilder => { metricsBuilder.AddMeter("OpenShock.Gateway"); })
    .AddOpenShockSignalR(redisOptions);

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IControlSender, ControlSender>();
builder.Services.AddScoped<IOtaService, OtaService>();
builder.Services.AddKeyedSingleton("OpenShock.Gateway.Meter", new Meter("OpenShock.Gateway", "1.0.0", [new KeyValuePair<string, object?>("gateway_fqdn", lcgOptions.Fqdn)]));

builder.AddSwaggerExt<Program>();

//services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddHostedService<LcgKeepAlive>();

builder.Services.AddSingleton<HubLifetimeManager>();

var app = builder.Build();

await app.UseCommonOpenShockMiddleware();

await app.RunAsync();