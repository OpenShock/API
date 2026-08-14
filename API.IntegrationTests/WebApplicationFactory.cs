using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using OpenShock.API.IntegrationTests.Docker;
using OpenShock.API.IntegrationTests.HttpMessageHandlers;
using Serilog;
using Serilog.Events;

namespace OpenShock.API.IntegrationTests;

// These tests exercise the API only. The API's job for transactional email is to write the
// EmailOutboxMessage row (and its business row) atomically - it never sends mail. Delivery, token
// minting, and newest-wins coalescing belong to the Cron host and are covered by Cron.IntegrationTests,
// so this factory deliberately boots no Cron host, no SMTP server, and no delivery loop.
public class WebApplicationFactory : WebApplicationFactory<Program>
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public required InMemoryDatabase PostgreSql { get; init; }

    [ClassDataSource<InMemoryRedis>(Shared = SharedType.PerTestSession)]
    public required InMemoryRedis Redis { get; init; }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        // BCrypt hashing in concurrent tests can starve the thread pool on CI runners,
        // causing in-process requests to queue far beyond the default 100 s timeout.
        client.Timeout = TimeSpan.FromMinutes(5);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // TODO: Find a way to do the following instead of the current implementation
        /*
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.Sources.Clear();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ...
            });
        });
        */
        var environmentVariables = new Dictionary<string, string>
        {
            { "ASPNETCORE_UNDER_INTEGRATION_TEST", "1" },

            { "OPENSHOCK__DB__CONN", PostgreSql.Container.GetConnectionString() },
            { "OPENSHOCK__DB__SKIPMIGRATION", "false" },
            { "OPENSHOCK__DB__DEBUG", "false" },

            { "OPENSHOCK__REDIS__CONN", Redis.Container.GetConnectionString() },

            { "OPENSHOCK__FRONTEND__BASEURL", "https://openshock.app" },
            { "OPENSHOCK__FRONTEND__SHORTURL", "https://openshock.app" },
            { "OPENSHOCK__FRONTEND__COOKIEDOMAIN", "openshock.app,localhost" },

            // The API never sends mail; it only reads the type to decide whether accounts wait for an
            // activation email. Any non-None value keeps the normal activation flow under test.
            // MailDisabledWebApplicationFactory covers the None behavior.
            { "OPENSHOCK__MAIL__TYPE", "Smtp" },

            { "OPENSHOCK__TURNSTILE__ENABLED", "true" },
            { "OPENSHOCK__TURNSTILE__SECRETKEY", "turnstile-secret-key" },
            { "OPENSHOCK__TURNSTILE__SITEKEY", "turnstile-site-key" },

            { "OPENSHOCK__LCG__FQDN", "de1-gateway.my-openshock-instance.net" },
            { "OPENSHOCK__LCG__COUNTRYCODE", "DE" }
        };

        foreach (var envVar in environmentVariables)
        {
            Environment.SetEnvironmentVariable(envVar.Key, envVar.Value);
        }

        builder.ConfigureServices(services =>
        {
            services.AddSerilog(configuration =>
            {
                configuration.WriteTo.Console(LogEventLevel.Warning);
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddTransient<HttpMessageHandlerBuilder, InterceptedHttpMessageHandlerBuilder>();

            // Disable rate limiting for integration tests so auth-endpoint tests
            // don't interfere with each other (10 req/min is too restrictive for test suites).
            // Rate limiter behavior is covered by dedicated unit tests.
            var rateLimiterDescriptors = services
                .Where(d => d.ServiceType.IsGenericType
                            && d.ServiceType.GetGenericTypeDefinition() == typeof(IConfigureOptions<>)
                            && d.ServiceType.GetGenericArguments()[0] == typeof(RateLimiterOptions))
                .ToList();
            foreach (var descriptor in rateLimiterDescriptors)
            {
                services.Remove(descriptor);
            }

            services.Configure<RateLimiterOptions>(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("test-no-limit"));
                options.AddPolicy("auth", _ =>
                    RateLimitPartition.GetNoLimiter("test-auth-no-limit"));
                options.AddPolicy("token-reporting", _ =>
                    RateLimitPartition.GetNoLimiter("test-token-reporting-no-limit"));
                options.AddPolicy("shocker-logs", _ =>
                    RateLimitPartition.GetNoLimiter("test-shocker-logs-no-limit"));
            });
        });
    }
}
