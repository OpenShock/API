using Microsoft.Net.Http.Headers;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace OpenShock.ServicesCommon.Utils
{
    public class OpenShockEnricher : ILogEventEnricher
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
        
            logEvent.AddOrUpdateProperty(new LogEventProperty("UserIp", new ScalarValue(ctx.Connection.RemoteIpAddress)));
            logEvent.AddOrUpdateProperty(new LogEventProperty("UserAgent", new ScalarValue(ctx.Request.Headers[HeaderNames.UserAgent].FirstOrDefault())));
            logEvent.AddOrUpdateProperty(new LogEventProperty("RequestHost", new ScalarValue(ctx.Request.Headers[HeaderNames.Host].FirstOrDefault())));
            logEvent.AddOrUpdateProperty(new LogEventProperty("RequestReferer", new ScalarValue(ctx.Request.Headers[HeaderNames.Referer].FirstOrDefault())));
            logEvent.AddOrUpdateProperty(new LogEventProperty("CF-Country", new ScalarValue(ctx.Request.Headers["CF-IPCountry"].FirstOrDefault())));
        
            //logEvent.AddOrUpdateProperty(new LogEventProperty("Headers", new DictionaryValue(ctx.Request.Headers.Select(x => new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(x.Key), new ScalarValue(x.Value))))));
        
            TryAddVar(logEvent, ctx, "User");
            TryAddVar(logEvent, ctx, "Device");
        }

        private void TryAddVar(LogEvent logEvent, HttpContext ctx, string name)
        {
            var user = ctx.Items[name];
            if (user == null) return;
            var propertyId = new LogEventProperty(name, new ScalarValue(user));
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
}