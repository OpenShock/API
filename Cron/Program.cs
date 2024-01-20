using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron;
using OpenShock.Cron.Jobs;
using OpenShock.ServicesCommon.Services.RedisPubSub;
using Quartz;
using Redis.OM;
using Redis.OM.Contracts;
using Serilog;
using StackExchange.Redis;

HostBuilder builder = new();
builder.UseContentRoot(Directory.GetCurrentDirectory())
    .ConfigureHostConfiguration(config =>
    {
        config.AddEnvironmentVariables(prefix: "DOTNET_");
        if (args is { Length: > 0 }) config.AddCommandLine(args);
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                reloadOnChange: false);

        config.AddEnvironmentVariables();
        if (args is { Length: > 0 }) config.AddCommandLine(args);
    })
    .UseDefaultServiceProvider((context, options) =>
    {
        var isDevelopment = context.HostingEnvironment.IsDevelopment();
        options.ValidateScopes = isDevelopment;
        options.ValidateOnBuild = isDevelopment;
    })
    .UseSerilog((context, _, config) => { config.ReadFrom.Configuration(context.Configuration); })
    .ConfigureServices((context, services) =>
    {
        
        
#if DEBUG
        var root = (IConfigurationRoot)context.Configuration;
        var debugView = root.GetDebugView();
        Console.WriteLine(debugView);
#endif
        var config = context.Configuration.GetChildren()
                                   .First(x => x.Key.Equals("openshock", StringComparison.InvariantCultureIgnoreCase))
                                   .Get<CronConf>() ??
                               throw new Exception("Couldn't bind config, check config file");
        
        // ----------------- DATABASE -----------------

        // How do I do this now with EFCore?!
#pragma warning disable CS0618
        NpgsqlConnection.GlobalTypeMapper.MapEnum<ControlType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<PermissionType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<ShockerModelType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<RankType>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<OtaUpdateStatus>();
#pragma warning restore CS0618
        services.AddDbContextPool<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(config.Db);
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        });

        services.AddDbContextFactory<OpenShockContext>(builder =>
        {
            builder.UseNpgsql(config.Db);
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        });

        // ----------------- REDIS -----------------

        var redisConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = true,
            Password = config.Redis.Password,
            User = config.Redis.User,
            Ssl = false,
            EndPoints = new EndPointCollection
            {
                { config.Redis.Host, config.Redis.Port }
            }
        };

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<IRedisPubService, RedisPubService>();

        services.AddMemoryCache();
        
        services.AddQuartz(q =>
        {
            q.ScheduleJob<OtaTimeoutJob>(t => t.WithCronSchedule("0 */1 * * * ?"));
        });
        services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
    });
try
{
    await builder.Build().RunAsync();
}
catch (Exception e)
{
    Console.WriteLine(e);
}