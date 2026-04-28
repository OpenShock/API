using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using OpenShock.API.IntegrationTests.Docker;
using OpenShock.API.IntegrationTests.Helpers;
using OpenShock.API.IntegrationTests.HttpMessageHandlers;
using Serilog;
using Serilog.Events;
using TUnit.Core.Interfaces;

namespace OpenShock.API.IntegrationTests;

public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public required InMemoryDatabase PostgreSql { get; init; }

    [ClassDataSource<InMemoryRedis>(Shared = SharedType.PerTestSession)]
    public required InMemoryRedis Redis { get; init; }

    [ClassDataSource<TestMailServer>(Shared = SharedType.PerTestSession)]
    public required TestMailServer Mailpit { get; init; }

    public MailpitHelper CreateMailpitHelper() => new(Mailpit.ApiBaseUrl);

    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
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

            { "OPENSHOCK__MAIL__TYPE", "SMTP" },
            { "OPENSHOCK__MAIL__SENDER__EMAIL", "system@openshock.org" },
            { "OPENSHOCK__MAIL__SENDER__NAME", "OpenShock" },
            { "OPENSHOCK__MAIL__SMTP__HOST", Mailpit.SmtpHost },
            { "OPENSHOCK__MAIL__SMTP__PORT", Mailpit.SmtpPort.ToString() },
            { "OPENSHOCK__MAIL__SMTP__ENABLESSL", "false" },
            { "OPENSHOCK__MAIL__SMTP__VERIFYCERTIFICATE", "false" },

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
