using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddShareRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE password_resets ALTER COLUMN used_on TYPE timestamp with time zone USING (CASE WHEN used_on IS NOT NULL THEN CURRENT_TIMESTAMP ELSE NULL END)");

            migrationBuilder.CreateTable(
                name: "share_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    user = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shares_codes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_share_requests_owner",
                        column: x => x.owner,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_share_requests_user",
                        column: x => x.user,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "share_requests_shockers",
                columns: table => new
                {
                    share_request = table.Column<Guid>(type: "uuid", nullable: false),
                    shocker = table.Column<Guid>(type: "uuid", nullable: false),
                    perm_sound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    perm_vibrate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    perm_shock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    limit_duration = table.Column<int>(type: "integer", nullable: true),
                    limit_intensity = table.Column<short>(type: "smallint", nullable: true),
                    perm_live = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("share_requests_shockers_pkey", x => new { x.share_request, x.shocker });
                    table.ForeignKey(
                        name: "fk_share_requests_shockers_share_request",
                        column: x => x.share_request,
                        principalTable: "share_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_share_requests_shockers_shocker",
                        column: x => x.shocker,
                        principalTable: "shockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_share_requests_owner",
                table: "share_requests",
                column: "owner")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_share_requests_user",
                table: "share_requests",
                column: "user");

            migrationBuilder.CreateIndex(
                name: "IX_share_requests_shockers_shocker",
                table: "share_requests_shockers",
                column: "shocker");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "share_requests_shockers");

            migrationBuilder.DropTable(
                name: "share_requests");

            migrationBuilder.Sql(
                "ALTER TABLE password_resets ALTER COLUMN used_on TYPE time with time zone USING used_on::time with time zone");

        }
    }
}
