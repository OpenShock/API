using System;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenShock.Common.OpenShockDb;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <inheritdoc />
public partial class AddControlStats : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "control_stats",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                shocker_id = table.Column<Guid>(type: "uuid", nullable: true),
                controller_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                bucket = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                type = table.Column<ControlType>(type: "control_type", nullable: false),
                live = table.Column<bool>(type: "boolean", nullable: false),
                count = table.Column<long>(type: "bigint", nullable: false),
                duration_ms = table.Column<long>(type: "bigint", nullable: false),
                intensity_ms = table.Column<long>(type: "bigint", nullable: false),
                max_intensity = table.Column<byte>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("control_stats_pkey", x => x.id);
                table.ForeignKey(
                    name: "fk_control_stats_owner_user_id",
                    column: x => x.owner_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_control_stats_shocker_id",
                    column: x => x.shocker_id,
                    principalTable: "shockers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_control_stats_controller_user_id_bucket",
            table: "control_stats",
            columns: new[] { "controller_user_id", "bucket" });

        migrationBuilder.CreateIndex(
            name: "IX_control_stats_owner_user_id_bucket",
            table: "control_stats",
            columns: new[] { "owner_user_id", "bucket" });

        migrationBuilder.CreateIndex(
            name: "IX_control_stats_shocker_id",
            table: "control_stats",
            column: "shocker_id");

        migrationBuilder.CreateIndex(
            name: "ix_control_stats_upsert_key",
            table: "control_stats",
            columns: new[] { "owner_user_id", "shocker_id", "controller_user_id", "bucket", "type", "live" },
            unique: true,
            filter: "shocker_id IS NOT NULL")
            .Annotation("Npgsql:NullsDistinct", false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "control_stats");
    }
}
