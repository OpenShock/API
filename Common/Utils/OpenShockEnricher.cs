using System.Security.Claims;
using Microsoft.Net.Http.Headers;
using OpenShock.Common.Authentication;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace OpenShock.Common.Utils;

public sealed class OpenShockEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _contextAccessor;

    public OpenShockEnricher() : this(new HttpContextAccessor())
    {
    }

    public OpenShockEnricher(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if(_contextAccessor.HttpContext == null) return;

        var ctx = _contextAccessor.HttpContext;
    
        logEvent.AddOrUpdateProperty(new LogEventProperty("UserIp", new ScalarValue(ctx.GetRemoteIP())));
        logEvent.AddOrUpdateProperty(new LogEventProperty("UserAgent", new ScalarValue(ctx.GetUserAgent())));
        logEvent.AddOrUpdateProperty(new LogEventProperty("RequestHost", new ScalarValue(ctx.Request.Headers[HeaderNames.Host].FirstOrDefault())));
        logEvent.AddOrUpdateProperty(new LogEventProperty("RequestReferer", new ScalarValue(ctx.Request.Headers[HeaderNames.Referer].FirstOrDefault())));
        logEvent.AddOrUpdateProperty(new LogEventProperty("CF-IPCountry", new ScalarValue(ctx.GetCFIPCountry())));
        
        foreach (var claim in ctx.User.Claims)
        {
            switch (claim.Type)
            {
                case ClaimTypes.NameIdentifier:
                    AddVar(logEvent, "User", claim.Value);
                    break;
                case OpenShockAuthClaims.ApiTokenId:
                    AddVar(logEvent, "ApiToken", claim.Value);
                    break;
                case OpenShockAuthClaims.HubId:
                    AddVar(logEvent, "Hub", claim.Value);
                    break;
            }
        }
    }

    private void AddVar(LogEvent logEvent, string key, string value)
    {
        var propertyId = new LogEventProperty(key, new ScalarValue(value));
        logEvent.AddOrUpdateProperty(propertyId);
    }
}

public static class OpenShockEnricherLoggerConfigurationExtensions
{
    public static LoggerConfiguration WithOpenShockEnricher(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        if (enrichmentConfiguration == null) throw new ArgumentNullException(nameof(enrichmentConfiguration));
        return enrichmentConfiguration.With<OpenShockEnricher>();
    }
}