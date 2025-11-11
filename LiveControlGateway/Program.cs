using Microsoft.Extensions.Options;
using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenAPI;
using OpenShock.Common.Services;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.Ota;
using OpenShock.LiveControlGateway;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Options;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args);

var redisOptions = builder.RegisterRedisOptions();
var databaseOptions = builder.RegisterDatabaseOptions();
builder.RegisterMetricsOptions();

// TODO Simplify this
builder.Services.Configure<LcgOptions>(builder.Configuration.GetRequiredSection(LcgOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<LcgOptions>, LcgOptionsValidator>();
builder.Services.AddSingleton<LcgOptions>(sp => sp.GetRequiredService<IOptions<LcgOptions>>().Value);

builder.Services
    .AddOpenShockMemDB(redisOptions)
    .AddOpenShockDB(databaseOptions)
    .AddOpenShockServices()
    .AddOpenShockSignalR(redisOptions);

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IControlSender, ControlSender>();
builder.Services.AddScoped<IOtaService, OtaService>();

builder.Services.AddOpenApiExt<Program>();

//services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddHostedService<LcgKeepAlive>();

builder.Services.AddSingleton<HubLifetimeManager>();

var app = builder.Build();

await app.UseCommonOpenShockMiddleware();

await app.RunAsync();