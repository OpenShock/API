extern alias cronhost;

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
using EmailOutboxDeliveryJob = cronhost::OpenShock.Cron.Jobs.EmailOutboxDeliveryJob;

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

    // The Cron host, booted in-process so the email outbox is actually drained and delivered. It reads
    // the same OPENSHOCK__* environment variables this factory sets (shared DB / Redis / Mailpit), so
    // it sees the same outbox rows the API writes.
    private CronHost? _cronHost;

    // Drives the email-outbox delivery job in the test host. In production the job runs on a recurring
    // every-minute Hangfire sweep (registered in Cron/Program.cs *after* WebApplication.Build()) plus an
    // on-demand enqueue from the Redis "pending" nudge. WebApplicationFactory stops the Cron host at
    // Build(), so that post-build recurring registration never runs here and a per-minute sweep would be
    // far too coarse for the Mailpit wait window anyway. To keep delivery deterministic we run the very
    // same job directly on a fast loop; it is built to run concurrently (FOR UPDATE SKIP LOCKED + an
    // attempt_count fencing token),
    // so it composes safely with the still-live notification listener.
    private CancellationTokenSource? _deliveryPumpCts;
    private Task? _deliveryPump;

    /// <summary>Service provider of the in-process Cron host (email dispatcher, outbox job, etc.).</summary>
    public IServiceProvider CronServices => _cronHost?.Services
        ?? throw new InvalidOperationException("Cron host not initialized");

    public Task InitializeAsync()
    {
        _ = Server; // Boots the API host, which sets the shared OPENSHOCK__* environment variables.
        _cronHost = new CronHost();
        _ = _cronHost.Services; // Boots the Cron host: the outbox delivery job + notification listener run.
        StartOutboxDeliveryPump(_cronHost.Services);
        return Task.CompletedTask;
    }

    private void StartOutboxDeliveryPump(IServiceProvider cronServices)
    {
        _deliveryPumpCts = new CancellationTokenSource();
        var cancellationToken = _deliveryPumpCts.Token;

        _deliveryPump = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = cronServices.CreateAsyncScope();
                    var job = ActivatorUtilities.CreateInstance<EmailOutboxDeliveryJob>(scope.ServiceProvider);
                    await job.Execute();
                }
                catch
                {
                    // Best effort: a transient hiccup (e.g. a row reclaimed by the live notification
                    // listener mid-batch) is simply picked up again on the next tick.
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _deliveryPumpCts?.Cancel();
            try
            {
                _deliveryPump?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected when the pump is cancelled on shutdown.
            }
            _deliveryPumpCts?.Dispose();
            _cronHost?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>Minimal factory that boots the Cron host (<c>cronhost::Program</c>) for delivery.</summary>
    private sealed class CronHost : WebApplicationFactory<cronhost::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSerilog(configuration => configuration.WriteTo.Console(LogEventLevel.Warning));
            });
        }
    }

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

            // Tests only: make the Cron host's Hangfire workers pick up enqueued jobs fast. The Redis
            // pending notification enqueues the email delivery job the instant a row is written, so this
            // is what lands mail inside the Mailpit wait window. Production keeps Hangfire's 15s default.
            { "OPENSHOCK__HANGFIRE__QUEUEPOLLINTERVAL", "00:00:00.5" },

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
