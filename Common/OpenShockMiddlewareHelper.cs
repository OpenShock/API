using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using OpenShock.Common.Options;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Scalar.AspNetCore;
using Serilog;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace OpenShock.Common;

public static class OpenShockMiddlewareHelper
{
    private static readonly ForwardedHeadersOptions ForwardedSettings = new()
    {
        ForwardedHeaders = ForwardedHeaders.All,
        RequireHeaderSymmetry = false,
        ForwardLimit = null,
        ForwardedForHeaderName = "CF-Connecting-IP"
    };
    
    public static IApplicationBuilder UseCommonOpenShockMiddleware(this WebApplication app)
    {
        var metricsOptions = app.Services.GetRequiredService<IOptions<MetricsOptions>>().Value;

        foreach (var proxy in TrustedProxiesFetcher.GetTrustedNetworks())
        {
            ForwardedSettings.KnownNetworks.Add(proxy);
        }

        app.UseForwardedHeaders(ForwardedSettings);
        
        app.UseSerilogRequestLogging();
        
        // Enable request body buffering. Needed to allow rewinding the body reader,
        // if the body has already been read before.
        // Runs before the request action is executed and body is read.
        app.Use((context, next) =>
        {
            context.Request.EnableBuffering();
            return next.Invoke();
        });
        app.UseExceptionHandler();

        // global cors policy
        app.UseCors();
        
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1)
        });
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Redis
        var redisConnection = app.Services.GetRequiredService<IRedisConnectionProvider>().Connection;

        redisConnection.CreateIndex(typeof(LoginSession));
        redisConnection.CreateIndex(typeof(DeviceOnline));
        redisConnection.CreateIndex(typeof(DevicePair));
        redisConnection.CreateIndex(typeof(LcgNode));

        var metricsAllowedIpNetworks = metricsOptions.AllowedNetworks.Select(x => IPNetwork.Parse(x));

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context =>
        {
            if(context.Request.Path != "/metrics") return false;
            
            var remoteIp = context.Connection.RemoteIpAddress;
            return remoteIp != null && metricsAllowedIpNetworks.Any(x => x.Contains(remoteIp));
        });
        
        app.MapOpenApi()
            .CacheOutput();
        
        app.MapScalarApiReference("/openapi/scalar", options =>
            options
                .WithOpenApiRoutePattern("/openapi/{documentName}.json")
                .AddDocument("1", "Version 1")
                .AddDocument("2", "Version 2")
        );
        
        app.MapControllers();
        
        return app;
    }
}