using OpenShock.Common.Options;
using StackExchange.Redis;

namespace OpenShock.Common.Extensions;

public static class ConfigurationExtensions
{
    // Reusable helper: bind or throw with a clear message
    private static T GetRequired<T>(this ConfigurationManager configuration, string path, string? example = null) where T : class
    {
        var section = configuration.GetSection(path);
        var value = section.Get<T>();
        if (value is null)
        {
            var hint = string.IsNullOrWhiteSpace(example) ? "" : $" Example: {example}";
            throw new InvalidOperationException(
                $"Missing or invalid configuration at '{path}'.{hint}"
            );
        }
        return value;
    }

    public static DatabaseOptions RegisterDatabaseOptions(this WebApplicationBuilder builder)
    {
        // If DatabaseOptions has required fields, you can validate them after binding.
        var options = builder.Configuration.GetRequired<DatabaseOptions>(
            "OpenShock:DB",
            """{ "ConnectionString": "..." }"""
        );

        builder.Services.AddSingleton(options);
        return options;
    }

    private sealed record RedisSection(string? Conn, string? User, string? Password, string? Host, string? Port);
    public static ConfigurationOptions RegisterRedisOptions(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetRequired<RedisSection>(
            "OpenShock:Redis",
            """{ "Conn": "redis://user:pass@host:6379" } or { "Host": "host", "Password": "...", "Port": "6379" }"""
        );

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
                    throw new ArgumentException("Redis Port must be a number between 1 and 65535 (OpenShock:Redis:Port).");
            }

            options = new ConfigurationOptions
            {
                User = section.User,              // optional; only if ACLs enabled
                Password = section.Password,
                Ssl = false,                  // flip via connection string if needed
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
        var options = builder.Configuration.GetRequired<MetricsOptions>(
            "OpenShock:Metrics"
        );

        builder.Services.AddSingleton(options);
        return options;
    }

    public static FrontendOptions RegisterFrontendOptions(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetRequired<FrontendOptions>(
            "OpenShock:Frontend"
        );

        builder.Services.AddSingleton(options);
        return options;
    }
}
