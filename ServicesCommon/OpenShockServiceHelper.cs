using OpenShock.ServicesCommon.Config;
using OpenShock.ServicesCommon.Services.BatchUpdate;
using OpenShock.ServicesCommon.Services.RedisPubSub;
using Redis.OM;
using Redis.OM.Contracts;
using StackExchange.Redis;

namespace OpenShock.ServicesCommon;

public static class OpenShockServiceHelper
{
    /// <summary>
    /// Register all OpenShock services for PRODUCTION use
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static ServicesResult AddOpenShockServices(this IServiceCollection services, BaseConfig config)
    {
        ConfigurationOptions configurationOptions;

        if (string.IsNullOrWhiteSpace(config.Redis.Conn))
        {
            if (string.IsNullOrWhiteSpace(config.Redis.Host))
                throw new Exception("You need to specify either OPENSHOCK__REDIS__CONN or OPENSHOCK__REDIS__HOST");

            configurationOptions = new ConfigurationOptions
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
        }
        else
        {
            configurationOptions = ConfigurationOptions.Parse(config.Redis.Conn);
        }

        configurationOptions.AbortOnConnectFail = true;

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configurationOptions));
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<IRedisPubService, RedisPubService>();

        services.AddSingleton<IBatchUpdateService, BatchUpdateService>();
        services.AddHostedService<BatchUpdateService>(provider =>
            (BatchUpdateService)provider.GetRequiredService<IBatchUpdateService>());


        return new ServicesResult
        {
            RedisConfig = configurationOptions
        };
    }

    public readonly struct ServicesResult
    {
        public ConfigurationOptions RedisConfig { get; init; }
    }
}