using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Redis.OM;
using Redis.OM.Contracts;
using Serilog;
using ShockLink.API.ExceptionHandle;
using ShockLink.API.Utils;
using ShockLink.Common.ShockLinkDb;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;

namespace ShockLink.API;

public class Startup
{
    private readonly ForwardedHeadersOptions _forwardedSettings = new()
    {
        ForwardedHeaders = ForwardedHeaders.All,
        RequireHeaderSymmetry = false,
        ForwardLimit = null
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContextPool<ShockLinkContext>(builder =>
        {
            builder.UseNpgsql(ApiConfig.Db);
            builder.EnableSensitiveDataLogging();
        });

        var redis = new RedisConnectionProvider($"redis://:{ApiConfig.RedisPassword}@{ApiConfig.RedisHost}:6379");
        services.AddSingleton<IRedisConnectionProvider>(redis);

        var redisConf = new RedisConfiguration
        {
            AbortOnConnectFail = true,
            Hosts = new[]
            {
                new RedisHost
                {
                    Host = ApiConfig.RedisHost,
                    Port = 6379
                }
            },
            Database = 0,
            Password = ApiConfig.RedisPassword
        };
        services.AddStackExchangeRedisExtensions<NewtonsoftSerializer>(redisConf);

        services.AddMemoryCache();
        services.AddHttpContextAccessor();


        
        services.AddWebEncoders();
        services.TryAddSingleton<ISystemClock, SystemClock>();

        services.AddControllers();

        services.AddCors();
        services.AddApiVersioning();
        
        //services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
    }

    private static readonly string[] CloudflareProxies =
    {
        "103.21.244.0/22", "103.22.200.0/22", "103.31.4.0/22", "104.16.0.0/13", "104.24.0.0/14", "108.162.192.0/18",
        "131.0.72.0/22", "141.101.64.0/18", "162.158.0.0/15", "172.64.0.0/13", "173.245.48.0/20", "188.114.96.0/20",
        "190.93.240.0/20", "197.234.240.0/22", "198.41.128.0/17", "2400:cb00::/32", "2606:4700::/32", "2803:f800::/32",
        "2405:b500::/32", "2405:8100::/32", "2c0f:f248::/32", "2a06:98c0::/29"
    };

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        Console.WriteLine("still here");
        
        foreach (var proxy in CloudflareProxies)
        {
            var split = proxy.Split('/');
            var ip = split[0];
            var mask = int.Parse(split[1]);
            _forwardedSettings.KnownNetworks.Add(new IPNetwork(IPAddress.Parse(ip), mask));
        }

        app.UseForwardedHeaders(_forwardedSettings);
        app.UseSerilogRequestLogging();
        ApplicationLogging.LoggerFactory = loggerFactory;

        app.ConfigureExceptionHandler();

        // global cors policy
        app.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowAnyOrigin()
            .SetPreflightMaxAge(TimeSpan.FromHours(24)));

        var redisConfiguration = new ConfigurationOptions
        {
            EndPoints = { { ApiConfig.RedisHost, 6379 } },
            Password = ApiConfig.RedisPassword,
            DefaultDatabase = 0,
            ClientName = "abi-api"
        };

        // PubSubManager.Initialize(ConnectionMultiplexer.Connect(redisConfiguration), app.ApplicationServices);
        
        var webSocketOptions = new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1)
        };
        
        app.UseWebSockets(webSocketOptions);
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            /*endpoints.MapHealthChecks("/{version:apiVersion}/public/healthcheck",
                new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    ResponseWriter = UiResponseWriter.WriteHealthCheckUiResponse
                });*/
            endpoints.MapControllers();
        });
    }
}