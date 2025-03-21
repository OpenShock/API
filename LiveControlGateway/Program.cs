using OpenShock.Common;
using OpenShock.Common.Extensions;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Services.Device;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Swagger;
using OpenShock.LiveControlGateway;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.PubSub;

var builder = OpenShockApplication.CreateDefaultBuilder<Program>(args, options =>
{
#if DEBUG
    options.ListenAnyIP(580);
    options.ListenAnyIP(5443, options => options.UseHttps("devcert.pfx"));
#else
    options.ListenAnyIP(80);
#endif
});

builder.RegisterCommonOpenShockOptions();

var databaseConfig = builder.Configuration.GetDatabaseOptions();
var redisConfig = builder.Configuration.GetRedisConfigurationOptions();

builder.Services.AddOpenShockServices(databaseConfig, redisConfig);

builder.Services.AddSignalR()
    .AddOpenShockStackExchangeRedis(options => { options.Configuration = redisConfig; })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
        options.PayloadSerializerOptions.Converters.Add(new SemVersionJsonConverter());
    });

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IOtaService, OtaService>();

builder.Services.AddSwaggerExt<Program>();

//services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddHostedService<RedisSubscriberService>();
builder.Services.AddHostedService<LcgKeepAlive>();

builder.Services.AddSingleton<HubLifetimeManager>();

var app = builder.Build();

app.UseCommonOpenShockMiddleware();

app.UseSwaggerExt();

app.MapControllers();

app.Run();