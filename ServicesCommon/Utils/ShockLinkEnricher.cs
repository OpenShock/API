using Microsoft.Net.Http.Headers;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace OpenShock.ServicesCommon.Utils
{
    public class ShockLinkEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public ShockLinkEnricher() : this(new HttpContextAccessor())
        {
        }

        public ShockLinkEnricher(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if(_contextAccessor.HttpContext == null) return;

            var ctx = _contextAccessor.HttpContext;
        
            logEvent.AddOrUpdateProperty(new LogEventProperty("UserIp", new ScalarValue(ctx.Connection.RemoteIpAddress)));
            logEvent.AddOrUpdateProperty(new LogEventProperty("UserAgent", new ScalarValue(ctx.Request.Headers[HeaderNames.UserAgent].FirstOrDefault())));
            logEvent.AddOrUpdateProperty(new LogEventProperty("RequestHost", new ScalarValue(ctx.Request.Headers[HeaderNames.Host].FirstOrDefault())));
            logEvent.AddOrUpdateProperty(new LogEventProperty("RequestReferer", new ScalarValue(ctx.Request.Headers[HeaderNames.Referer].FirstOrDefault())));
        
            //logEvent.AddOrUpdateProperty(new LogEventProperty("Headers", new DictionaryValue(ctx.Request.Headers.Select(x => new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(x.Key), new ScalarValue(x.Value))))));
        
            TryAddVar(logEvent, ctx, "User");
            TryAddVar(logEvent, ctx, "Device");
        }

        private void TryAddVar(LogEvent logEvent, HttpContext ctx, string name)
        {
            var user = ctx.Items[name];
            if (user == null) return;
            var correlationIdProperty = new LogEventProperty(name, new ScalarValue(user));
            logEvent.AddOrUpdateProperty(correlationIdProperty);
        }
    }

    public static class ShockLinkEnricherLoggerConfigurationExtensions
    {
        public static LoggerConfiguration WithShockLinkEnricher(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null) throw new ArgumentNullException(nameof(enrichmentConfiguration));
            return enrichmentConfiguration.With<ShockLinkEnricher>();
        }
    }
}