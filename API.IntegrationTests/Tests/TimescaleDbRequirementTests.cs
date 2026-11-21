using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using OpenShock.Common;
using OpenShock.Common.Options;
using OpenShock.Common.Services.ControlStats;
using Testcontainers.PostgreSql;

namespace OpenShock.API.IntegrationTests.Tests;

public sealed class TimescaleDbRequirementTests
{
    private const string Community = "timescale/timescaledb:latest-pg18";

    private static PostgreSqlContainer CreateContainer(string image) => new PostgreSqlBuilder(image: image).Build();

    private static string WithDatabase(PostgreSqlContainer container, string database) =>
        new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = database }.ConnectionString;

    private static async Task Psql(PostgreSqlContainer container, string database, string sql)
    {
        var result = await container.ExecAsync(["psql", "-U", PostgreSqlBuilder.DefaultUsername, "-d", database, "-c", sql]);
        if (result.ExitCode != 0) throw new InvalidOperationException(result.Stderr);
    }

    private static async Task<bool> HasExtension(PostgreSqlContainer container, string database)
    {
        await using var connection = new NpgsqlConnection(WithDatabase(container, database));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM pg_extension WHERE extname = 'timescaledb'", connection);
        return (long)(await command.ExecuteScalarAsync())! == 1;
    }

    private static async Task<bool> HasControlStatsPolicy(PostgreSqlContainer container, string database)
    {
        await using var connection = new NpgsqlConnection(WithDatabase(container, database));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT count(*) FROM timescaledb_information.jobs
            WHERE proc_name = 'policy_refresh_continuous_aggregate' AND hypertable_name = 'control_stats'
            """, connection);
        return (long)(await command.ExecuteScalarAsync())! == 1;
    }

    private static async Task<bool> HasMigrationHistory(PostgreSqlContainer container)
    {
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""SELECT to_regclass('"__EFMigrationsHistory"') IS NOT NULL""", connection);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task ApplyMigrations(string connectionString)
    {
        await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await new ApplicationBuilder(services).ApplyPendingOpenShockMigrations(new DatabaseOptions { Conn = connectionString });
    }

    [Test]
    public async Task Community_ApplyingMigrationsOnNewDatabase_Works()
    {
        await using var container = CreateContainer(Community);
        await container.StartAsync();

        await ApplyMigrations(WithDatabase(container, "created_on_startup"));
        await ControlStatsBackfillService.EnsureMaterializedAsync(WithDatabase(container, "created_on_startup"), NullLogger.Instance);

        await Assert.That(await HasExtension(container, "created_on_startup")).IsTrue();
        await Assert.That(await HasControlStatsPolicy(container, "created_on_startup")).IsTrue();
    }

    [Test]
    public async Task Backfill_MaterializesHistoryThenAddsPolicy()
    {
        await using var container = CreateContainer(Community);
        await container.StartAsync();
        var connectionString = WithDatabase(container, "backfill");

        await ApplyMigrations(connectionString);
        await Assert.That(await HasControlStatsPolicy(container, "backfill")).IsFalse();

        // Logs spanning several backfill windows, the owner foreign key is skipped instead of creating a user
        await Psql(container, "backfill",
            """
            SET session_replication_role = replica;
            INSERT INTO shocker_control_logs (id, owner_user_id, shocker_id, controlled_by_user_id, intensity, max_intensity, duration, type, custom_name, live_control, created_at)
            SELECT gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), NULL, 50, 50, 1000, 'shock', NULL, false, now() - age
            FROM unnest(ARRAY[INTERVAL '200 days', INTERVAL '40 days', INTERVAL '2 days']) AS age;
            """);

        await Assert.That(await ControlStatsBackfillService.EnsureMaterializedAsync(connectionString, NullLogger.Instance)).IsTrue();
        // A second run finds the policy and does nothing
        await Assert.That(await ControlStatsBackfillService.EnsureMaterializedAsync(connectionString, NullLogger.Instance)).IsTrue();

        await Assert.That(await HasControlStatsPolicy(container, "backfill")).IsTrue();

        // Only what is materialized, real-time aggregation would show the logs either way
        await Psql(container, "backfill", "ALTER MATERIALIZED VIEW control_stats SET (timescaledb.materialized_only = true)");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT coalesce(sum(count), 0) FROM control_stats", connection);
        await Assert.That(Convert.ToInt64(await command.ExecuteScalarAsync())).IsEqualTo(3);
    }

    [Test]
    public async Task Community_Check_LeavesNothingBehind()
    {
        await using var container = CreateContainer(Community);
        await container.StartAsync();
        await Psql(container, PostgreSqlBuilder.DefaultDatabase, "CREATE DATABASE fresh TEMPLATE template0");

        await OpenShockMiddlewareHelper.EnsureTimescaleDbAsync(WithDatabase(container, "fresh"));

        await Assert.That(await HasExtension(container, "fresh")).IsFalse();
    }

    [Test]
    [Arguments("postgres:18", "is not available")]
    [Arguments("timescale/timescaledb:latest-pg18-oss", "apache")]
    public async Task WrongEdition_RefusesToMigrate(string image, string reason)
    {
        await using var container = CreateContainer(image);
        await container.StartAsync();

        var exception = await Assert.That(() => ApplyMigrations(container.GetConnectionString()))
            .Throws<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains(reason);
        await Assert.That(await HasMigrationHistory(container)).IsFalse();
    }

    [Test]
    public async Task NotPreloaded_RefusesToMigrate()
    {
        await using var container = CreateContainer(Community);
        await container.StartAsync();
        await Psql(container, PostgreSqlBuilder.DefaultDatabase, "CREATE DATABASE fresh TEMPLATE template0");
        await Psql(container, PostgreSqlBuilder.DefaultDatabase, "CREATE DATABASE has_extension TEMPLATE template0");
        await Psql(container, "has_extension", "CREATE EXTENSION timescaledb");
        await Psql(container, PostgreSqlBuilder.DefaultDatabase, "ALTER SYSTEM SET shared_preload_libraries = 'pg_stat_statements'");
        await container.StopAsync();
        await container.StartAsync();

        foreach (var (database, reason) in new[] { ("fresh", "must be preloaded"), ("has_extension", "not preloaded") })
        {
            var exception = await Assert.That(() => ApplyMigrations(WithDatabase(container, database)))
                .Throws<InvalidOperationException>();

            await Assert.That(exception!.Message).Contains(reason);
        }
    }
}
