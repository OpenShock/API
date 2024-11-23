using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class FixPetrainer998DRRFIDs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                UPDATE shockers
                SET
                    rf_id = ((rf_id)::bit(32) << 1)::integer
                WHERE
                    model = 'petrainer998DR'
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                UPDATE shockers
                SET
                    rf_id = ((rf_id)::bit(32) >> 1)::integer
                WHERE
                    model = 'petrainer998DR'
                """
            );
        }
    }
}
