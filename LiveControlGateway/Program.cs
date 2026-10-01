using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.Services;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.Ota;
using OpenShock.LiveControlGateway;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Metrics;
using OpenShock.LiveControlGateway.Options;
using OpenShock.LiveControlGateway.PubSub;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

var redisOptions = builder.RegisterRedisOptions();
var databaseOptions = builder.RegisterDatabaseOptions();
builder.RegisterMetricsOptions();

var lcgOptions = builder.Configuration.GetRequiredSection(LcgOptions.SectionName).Get<LcgOptions>();
if (lcgOptions is null)
    throw new InvalidOperationException($"Missing or invalid configuration for {LcgOptions.SectionName}.");

// A lone latitude or longitude would silently fall back to country-level assignment, so fail loudly.
if (lcgOptions.Latitude.HasValue != lcgOptions.Longitude.HasValue)
    throw new InvalidOperationException($"{LcgOptions.SectionName}: Latitude and Longitude must be set together.");

builder.Services.AddSingleton<IValidateOptions<LcgOptions>, LcgOptionsValidator>();
builder.Services.AddSingleton(lcgOptions);

builder.Services
    .AddOpenShockMemDB(redisOptions)
    .AddOpenShockDB(databaseOptions)
    .AddOpenShockServices(configureMetrics: metricsBuilder => { metricsBuilder.AddMeter("OpenShock.Gateway", GatewayLoadSampler.ResourceMonitoringMeter); })
    .AddOpenShockSignalR(redisOptions);

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IControlSender, ControlSender>();
builder.Services.AddScoped<IOtaService, OtaService>();
// The fqdn rides on the individual measurements, not on the Meter: a tag set here is a *scope*
// attribute, which the Prometheus exporter emits prefixed as otel_scope_gateway_fqdn.
builder.Services.AddKeyedSingleton("OpenShock.Gateway.Meter", new Meter("OpenShock.Gateway", "1.0.0"));

builder.Services.AddHostedService<LcgKeepAlive>();

builder.Services.AddSingleton<GatewayMetrics>();
builder.Services.AddSingleton<HubLifetimeManager>();
builder.Services.AddResourceMonitoring();
// The defaults differ per OS ([0, 1] on Linux, [0, 100] on Windows), so pin both to [0, 1].
#pragma warning disable EXTEXP0008
builder.Services.Configure<ResourceMonitoringOptions>(options =>
{
    options.UseZeroToOneRangeForLinuxMetrics = true;
    options.UseZeroToOneRangeForMetrics = true;
});
#pragma warning restore EXTEXP0008
builder.Services.AddSingleton<GatewayLoadSampler>();
builder.Services.AddSingleton<ApiTokenUpdateSubscriber>();

var app = builder.Build();

await app.UseCommonOpenShockMiddleware(lcgOptions.PublicPath);

await app.WaitForOpenShockSchemaReady(databaseOptions);

await app.RunAsync();