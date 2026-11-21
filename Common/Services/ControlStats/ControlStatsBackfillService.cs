using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace OpenShock.Common.Services.ControlStats;

/// <summary>
/// Materializes the history of <c>control_stats</c> after startup instead of in a migration, which held up startup
/// for as long as it took to aggregate every control log. Until it is done stats stay correct, real-time aggregation
/// computes whatever isn't materialized yet from the logs. The refresh policy is only added once the history is
/// materialized: it moves the watermark to the present, so anything older left unmaterialized would drop out.
/// </summary>
public sealed class ControlStatsBackfillService : BackgroundService
{
    // Arbitrary, unique to this job, so only one instance backfills at a time
    private const long LockKey = 0x4F53_4353_4246; // "OSCSBF"

    private static readonly TimeSpan Window = TimeSpan.FromDays(30);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    private readonly string _connectionString;
    private readonly ILogger<ControlStatsBackfillService> _logger;

    public ControlStatsBackfillService(string connectionString, ILogger<ControlStatsBackfillService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureMaterializedAsync(_connectionString, _logger, stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Materializing control_stats failed, retrying in {Delay}", RetryDelay);
            }

            await Task.Delay(RetryDelay, stoppingToken);
        }
    }

    /// <summary>
    /// Materializes the history and adds the refresh policy, unless the policy already exists. Safe to run from
    /// several instances and to interrupt, a later run continues from what is already materialized.
    /// </summary>
    public static async Task EnsureMaterializedAsync(string connectionString, ILogger logger, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (await HasPolicyAsync(connection, cancellationToken)) return;

        if (!await ScalarAsync<bool>(connection, $"SELECT pg_try_advisory_lock({LockKey})", cancellationToken))
        {
            logger.LogInformation("control_stats is being materialized by another instance");
            return;
        }

        try
        {
            // Checked again, another instance may have finished between the check and taking the lock
            if (await HasPolicyAsync(connection, cancellationToken)) return;

            var stopwatch = Stopwatch.StartNew();
            await MaterializeHistoryAsync(connection, logger, cancellationToken);

            await ExecuteAsync(connection,
                """
                SELECT add_continuous_aggregate_policy('control_stats',
                    start_offset => INTERVAL '3 days',
                    end_offset => INTERVAL '1 hour',
                    schedule_interval => INTERVAL '5 minutes',
                    if_not_exists => true,
                    initial_start => now() + INTERVAL '5 minutes')
                """, cancellationToken);

            logger.LogInformation("Materialized control_stats in {Elapsed}", stopwatch.Elapsed);
        }
        finally
        {
            await ExecuteAsync(connection, $"SELECT pg_advisory_unlock({LockKey})", CancellationToken.None);
        }
    }

    /// <summary>
    /// Refreshes <c>control_stats</c> oldest first, one window at a time. Each refresh commits on its own, so the
    /// progress survives a restart.
    /// </summary>
    public static async Task MaterializeHistoryAsync(NpgsqlConnection connection, ILogger logger, CancellationToken cancellationToken = default)
    {
        // Bucket aligned, a refresh only covers buckets entirely inside its window
        DateTime? first;
        DateTime to;
        await using (var command = new NpgsqlCommand(
                         """
                         SELECT time_bucket(INTERVAL '15 minutes', min(created_at)),
                                time_bucket(INTERVAL '15 minutes', now() - INTERVAL '1 hour')
                         FROM shocker_control_logs
                         """, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            await reader.ReadAsync(cancellationToken);
            first = await reader.IsDBNullAsync(0, cancellationToken) ? null : reader.GetDateTime(0);
            to = reader.GetDateTime(1);
        }

        if (first is not { } from || from >= to)
        {
            logger.LogInformation("No control logs to materialize");
            return;
        }

        logger.LogInformation("Materializing control_stats from {From:u} to {To:u}", from, to);

        for (var start = from; start < to; start += Window)
        {
            var end = start + Window < to ? start + Window : to;
            await RefreshAsync(connection, start, end, cancellationToken);
            logger.LogInformation("Materialized control_stats up to {End:u}", end);
        }
    }

    private static async Task RefreshAsync(NpgsqlConnection connection, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var command = new NpgsqlCommand("CALL refresh_continuous_aggregate('control_stats', $1, $2)", connection);
                command.CommandTimeout = 0;
                command.Parameters.AddWithValue(start);
                command.Parameters.AddWithValue(end);
                await command.ExecuteNonQueryAsync(cancellationToken);
                return;
            }
            // Another refresh of the same range is running, TimescaleDB fails instead of waiting
            catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.LockNotAvailable && attempt < 10)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }
    }

    private static Task<bool> HasPolicyAsync(NpgsqlConnection connection, CancellationToken cancellationToken) =>
        ScalarAsync<bool>(connection,
            """
            SELECT EXISTS (SELECT 1 FROM timescaledb_information.jobs
                           WHERE proc_name = 'policy_refresh_continuous_aggregate' AND hypertable_name = 'control_stats')
            """, cancellationToken);

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = 0;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
