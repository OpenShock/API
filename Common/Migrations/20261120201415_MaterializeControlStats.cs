using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <summary>
/// Used to materialize the control_stats history and add its refresh policy here, which held up startup for as
/// long as aggregating every control log took. That moved to <c>ControlStatsBackfillService</c>, which does it
/// in the background and skips databases where this migration already did it.
/// </summary>
public partial class _20261120201415_MaterializeControlStats : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
