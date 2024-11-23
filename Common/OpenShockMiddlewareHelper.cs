using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using OpenShock.Common.Config;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM;
using Redis.OM.Contracts;
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
    
    public static IApplicationBuilder UseCommonOpenShockMiddleware(this IApplicationBuilder app)
    {
        var config = app.ApplicationServices.GetRequiredService<BaseConfig>();
        
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
        var redisConnection = app.ApplicationServices.GetRequiredService<IRedisConnectionProvider>().Connection;

        redisConnection.CreateIndex(typeof(LoginSession));
        redisConnection.CreateIndex(typeof(DeviceOnline));
        redisConnection.CreateIndex(typeof(DevicePair));
        redisConnection.CreateIndex(typeof(LcgNode));

        var metricsAllowedIpNetworks = config.Metrics.AllowedNetworks.Select(x => IPNetwork.Parse(x));
        
        app.UseOpenTelemetryPrometheusScrapingEndpoint(context =>
        {
            if(context.Request.Path != "/metrics") return false;
            
            var remoteIp = context.Connection.RemoteIpAddress;
            return remoteIp != null && metricsAllowedIpNetworks.Any(x => x.Contains(remoteIp));
        });
        
        return app;
    }
}