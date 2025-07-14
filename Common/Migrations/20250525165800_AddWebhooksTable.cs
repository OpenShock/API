using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhooksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discord_webhooks",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_id = table.Column<long>(type: "bigint", nullable: false),
                    webhook_token = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("discord_webhooks_pkey", x => x.name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discord_webhooks");
        }
    }
}
