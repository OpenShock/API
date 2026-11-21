using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <summary>
/// Separate from <see cref="_20261120201327_AddControlLogHypertable"/> as it can't run in a transaction.
/// </summary>
public partial class _20261120201415_MaterializeControlStats : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "CALL refresh_continuous_aggregate('control_stats', NULL, now() - INTERVAL '1 hour');",
            suppressTransaction: true);

        // Added after the refresh above, which fails if the policy's job is refreshing at the same time
        migrationBuilder.Sql(
            """
            SELECT add_continuous_aggregate_policy('control_stats',
                start_offset => INTERVAL '3 days',
                end_offset => INTERVAL '1 hour',
                schedule_interval => INTERVAL '5 minutes',
                if_not_exists => true,
                initial_start => now() + INTERVAL '5 minutes');
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
