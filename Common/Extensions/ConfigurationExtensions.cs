using OpenShock.Common.Options;
using OpenShock.Common.Utils;
using StackExchange.Redis;

namespace OpenShock.Common.Extensions;

file sealed class RedisSection
{
    public string? Conn { get; init; }
    public string? User { get; init; }
    public string? Password { get; init; }
    public string? Host { get; init; }
    public string? Port { get; init; }
}

public static class ConfigurationExtensions
{
    public static DatabaseOptions RegisterDatabaseOptions(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetRequiredSection("OpenShock:DB").Get<DatabaseOptions>();
        if (options is null) 
            throw new InvalidOperationException("Missing or invalid configuration for OpenShock:DB.");
        
        if (string.IsNullOrEmpty(options.Conn)) throw new InvalidOperationException("Missing or invalid connection string (OpenShock:DB:Conn).");

        builder.Services.AddSingleton(options);
        return options;
    }

    public static ConfigurationOptions RegisterRedisOptions(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequiredSection("OpenShock:Redis").Get<RedisSection>();
        if (section is null) 
            throw new InvalidOperationException("Missing or invalid configuration for OpenShock:Redis.");

        ConfigurationOptions options;

        if (!string.IsNullOrEmpty(section.Conn))
        {
            options = ConfigurationOptions.Parse(section.Conn);
        }
        else
        {
            if (string.IsNullOrEmpty(section.Host)) throw new InvalidOperationException("Redis Host field is required if no connectionstring is specified (OpenShock:Redis:Host).");

            // Parse port with sane default + validation
            ushort port = 6379;
            if (!string.IsNullOrWhiteSpace(section.Port))
            {
                if (!ushort.TryParse(section.Port, out port) || port == 0)
                    throw new InvalidOperationException("Redis Port must be a number between 1 and 65535 (OpenShock:Redis:Port).");
            }

            options = new ConfigurationOptions
            {
                User = section.User ?? string.Empty,
                Password = section.Password ?? string.Empty,
                Ssl = false,
                EndPoints = { { section.Host, port } },
            };
        }

        // Sensible defaults (adjust to taste)
        options.AbortOnConnectFail = true;

        builder.Services.AddSingleton(options);
        return options;
    }

    public static MetricsOptions RegisterMetricsOptions(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection("OpenShock:Metrics").Get<MetricsOptions>();

        options = new MetricsOptions
        {
            AllowedNetworks = options?.AllowedNetworks.Count is > 0 ? options.AllowedNetworks : TrustedProxiesFetcher.PrivateNetworks
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
            CookieDomains = ParseDomainList(section["CookieDomain"] ?? throw new InvalidOperationException("Frontend CookieDomain is required (OpenShock:Frontend:CookieDomain).")),
        };
        
        if (options.CookieDomains.Count == 0) throw new InvalidOperationException("At least one cookie domain must be configured (OpenShock:Frontend:CookieDomain).");

        builder.Services.AddSingleton(options);
        return options;

        static string[] ParseDomainList(string csv)
        {
            var entries = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (var i = 0; i < entries.Length; i++)
            {
                var domain = entries[i];

                // leave localhost alone
                if (string.Equals(domain, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    entries[i] = "localhost";
                    continue;
                }

                // leave IP addresses alone
                if (System.Net.IPAddress.TryParse(domain, out _)) continue;
                
                if (!DomainUtils.IsValidDomain(domain))
                    throw new Exception($"Invalid domain: {domain}");

                // normalize FQDN: ensure it starts with a dot
                if (!domain.StartsWith('.'))
                    domain = "." + domain.ToLowerInvariant();

                entries[i] = domain;
            }

            return entries;
        }
    }
}
