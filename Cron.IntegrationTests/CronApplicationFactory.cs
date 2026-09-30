using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.IntegrationTests.Docker;
using OpenShock.Cron.IntegrationTests.Helpers;
using OpenShock.Cron.Jobs;
using Serilog;
using Serilog.Events;
using TUnit.Core.Interfaces;

namespace OpenShock.Cron.IntegrationTests;

/// <summary>
/// Boots the real Cron host (its <c>Program</c>) against throwaway Postgres / Redis / Mailpit
/// containers. Delivery is driven deterministically: tests seed outbox rows and then call
/// <see cref="RunDeliveryAsync"/> to run one pass of the real <see cref="EmailOutboxDeliveryJob"/> -
/// there is no background loop, so nothing races the assertions. The Cron host doesn't migrate on
/// boot, so the factory applies migrations to the fresh database first.
/// </summary>
public class CronApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public required InMemoryDatabase PostgreSql { get; init; }

    [ClassDataSource<InMemoryRedis>(Shared = SharedType.PerTestSession)]
    public required InMemoryRedis Redis { get; init; }

    [ClassDataSource<TestMailServer>(Shared = SharedType.PerTestSession)]
    public required TestMailServer Mailpit { get; init; }

    public MailpitHelper CreateMailpitHelper() => new(Mailpit.ApiBaseUrl);

    public IDbContextFactory<OpenShockContext> DbContextFactory =>
        Services.GetRequiredService<IDbContextFactory<OpenShockContext>>();

    public async Task InitializeAsync()
    {
        // The Cron host doesn't run migrations on boot, so apply them to the fresh database before
        // anything touches it. Uses the same connection string the host will read from the env var.
        await using (var migrationContext = new MigrationOpenShockContext(
                         PostgreSql.Container.GetConnectionString(), false, NullLoggerFactory.Instance))
        {
            await migrationContext.Database.MigrateAsync();
        }

        _ = Services; // Boot the Cron host (email dispatcher, templates, db factory, etc.).
    }

    /// <summary>
    /// Runs one pass of the real delivery job in a fresh DI scope. Deterministic: it returns only
    /// after the batch (claim → dispatch → record outcome) has completed.
    /// </summary>
    public async Task RunDeliveryAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var job = ActivatorUtilities.CreateInstance<EmailOutboxDeliveryJob>(scope.ServiceProvider);
        await job.Execute();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var environmentVariables = new Dictionary<string, string>
        {
            { "ASPNETCORE_UNDER_INTEGRATION_TEST", "1" },

            { "OPENSHOCK__DB__CONN", PostgreSql.Container.GetConnectionString() },
            { "OPENSHOCK__DB__SKIPMIGRATION", "true" },
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
        };

        foreach (var envVar in environmentVariables)
        {
            Environment.SetEnvironmentVariable(envVar.Key, envVar.Value);
        }

        builder.ConfigureServices(services =>
        {
            services.AddSerilog(configuration => configuration.WriteTo.Console(LogEventLevel.Warning));
        });
    }
}
