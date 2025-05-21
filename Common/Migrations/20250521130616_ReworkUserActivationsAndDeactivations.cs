using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class ReworkUserActivationsAndDeactivations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "activated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // Set 'activated_at' based on 'user_activations.created_at', or fallback to 'users.created_at'
            migrationBuilder.Sql(
                """
                UPDATE users
                SET activated_at = CASE
                    WHEN email_activated THEN ua.created_at
                    ELSE NULL
                END
                FROM (
                    SELECT user_id, MIN(created_at) AS created_at
                    FROM user_activations
                    GROUP BY user_id
                ) ua
                WHERE users.id = ua.user_id;
                """
             );

            migrationBuilder.DropColumn(
                name: "email_activated",
                table: "users");

            // Delete activation records if the account has been activated
            migrationBuilder.Sql(
                """
                DELETE FROM user_activations
                WHERE user_id IN (
                    SELECT id FROM users WHERE activated_at IS NOT NULL
                );
                """
            );

            migrationBuilder.DropForeignKey(
                name: "fk_user_activations_user_id",
                table: "user_activations");

            migrationBuilder.DropPrimaryKey(
                name: "user_activations_pkey",
                table: "user_activations");

            migrationBuilder.DropIndex(
                name: "IX_user_activations_user_id",
                table: "user_activations");

            migrationBuilder.DropColumn(
                name: "id",
                table: "user_activations");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "user_activations");

            migrationBuilder.AddColumn<int>(
                name: "email_send_attempts",
                table: "user_activations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.RenameTable(
                name: "user_activations",
                newName: "user_activation_requests");

            migrationBuilder.AddPrimaryKey(
                name: "user_activation_requests_pkey",
                table: "user_activation_requests",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_activation_requests_user_id",
                table: "user_activation_requests",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateTable(
                name: "user_deactivations",
                columns: table => new
                {
                    deactivated_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deactivated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delete_later = table.Column<bool>(type: "boolean", nullable: false),
                    user_moderation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_deactivations_pkey", x => x.deactivated_user_id);
                    table.ForeignKey(
                        name: "fk_user_deactivations_deactivated_by_user_id",
                        column: x => x.deactivated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_deactivations_deactivated_user_id",
                        column: x => x.deactivated_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_deactivations_deactivated_by_user_id",
                table: "user_deactivations",
                column: "deactivated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_deactivations");

            migrationBuilder.DropForeignKey(
                name: "fk_user_activation_requests_user_id",
                table: "user_activation_requests");

            migrationBuilder.DropPrimaryKey(
                name: "user_activation_requests_pkey",
                table: "user_activation_requests");

            migrationBuilder.RenameTable(
                name: "user_activation_requests",
                newName: "user_activations");

            migrationBuilder.DropColumn(
                name: "email_send_attempts",
                table: "user_activations");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "user_activations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "used_at",
                table: "user_activations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "user_activations_pkey",
                table: "user_activations",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_user_activations_user_id",
                table: "user_activations",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_activations_user_id",
                table: "user_activations",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddColumn<bool>(
                name: "email_activated",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE users
                SET email_activated = CASE
                    WHEN activated_at IS NOT NULL THEN TRUE
                    ELSE FALSE
                END;
                """
            );

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "users");
        }
    }
}
