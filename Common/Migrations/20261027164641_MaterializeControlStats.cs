using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <summary>
/// Separate from <see cref="AddControlLogHypertable"/> as it can't run in a transaction.
/// </summary>
public partial class MaterializeControlStats : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "CALL refresh_continuous_aggregate('control_stats', NULL, now() - INTERVAL '1 hour');",
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
