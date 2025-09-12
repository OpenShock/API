using OpenShock.Common.Options;
using OpenShock.Common.Utils;
using StackExchange.Redis;

namespace OpenShock.Common.Extensions;

public static class ConfigurationExtensions
{
    public static DatabaseOptions RegisterDatabaseOptions(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetRequiredSection("OpenShock:DB").Get<DatabaseOptions>();
        if (options is null) 
            throw new InvalidOperationException("Missing or invalid configuration for OpenShock:DB.");

        builder.Services.AddSingleton(options);
        return options;
    }

    private sealed class RedisSection
    {
        public string? Conn { get; init; }
        public string? User { get; init; }
        public string? Password { get; init; }
        public string? Host { get; init; }
        public string? Port { get; init; }
    }

    public static ConfigurationOptions RegisterRedisOptions(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection("OpenShock:Redis").Get<RedisSection>();
        if (section is null) 
            throw new InvalidOperationException("Missing or invalid configuration for OpenShock:Redis.");

        ConfigurationOptions options;

        if (!string.IsNullOrWhiteSpace(section.Conn))
        {
            options = ConfigurationOptions.Parse(section.Conn);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(section.Host))
                throw new ArgumentException("Redis Host is required (OpenShock:Redis:Host).");

            if (string.IsNullOrWhiteSpace(section.Password))
                throw new ArgumentException("Redis Password is required (OpenShock:Redis:Password).");

            // Parse port with sane default + validation
            ushort port = 6379;
            if (!string.IsNullOrWhiteSpace(section.Port))
            {
                if (!ushort.TryParse(section.Port, out port) || port == 0)
                    throw new InvalidOperationException("Redis Port must be a number between 1 and 65535 (OpenShock:Redis:Port).");
            }

            options = new ConfigurationOptions
            {
                User = section.User,              // optional; only if ACLs enabled
                Password = section.Password,
                Ssl = false,                      // flip via connection string if needed
            };
            options.EndPoints.Add(section.Host!, port);
        }

        // Sensible defaults (adjust to taste)
        options.AbortOnConnectFail = true;

        builder.Services.AddSingleton(options);
        return options;
    }

    public static MetricsOptions RegisterMetricsOptions(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection("OpenShock:Metrics").Get<MetricsOptions>() ?? new MetricsOptions
        {
            AllowedNetworks = TrustedProxiesFetcher.PrivateNetworks
        };

        builder.Services.AddSingleton(options);
        return options;
    }

    public static FrontendOptions RegisterFrontendOptions(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection("OpenShock:Frontend");
        
        var options = new FrontendOptions
        {
            BaseUrl = section.GetValue<Uri>("BaseUrl") ?? throw new InvalidOperationException("Frontend BaseUrl is required (OpenShock:Frontend:BaseUrl)."),
            ShortUrl = section.GetValue<Uri>("ShortUrl") ?? throw new InvalidOperationException("Frontend ShortUrl is required (OpenShock:Frontend:ShortUrl)."),
            CookieDomains = SplitCsv(section["CookieDomain"] ?? throw new InvalidOperationException("Frontend CookieDomain is required (OpenShock:Frontend:CookieDomain).")),
        };

        builder.Services.AddSingleton(options);
        return options;

        static string[] SplitCsv(string csv)
        {
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
