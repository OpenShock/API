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
                UPDATE users SET activated_at = CASE
                  WHEN email_activated THEN COALESCE(
                    (SELECT MIN(ua.used_at) FROM user_activations ua WHERE ua.user_id = users.id),
                    users.created_at
                  )
                  ELSE NULL
                END;
                """
             );

            migrationBuilder.Sql("DROP VIEW admin_users_view;");

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

            // Recreate the admin_users_view
            migrationBuilder.Sql(
                """
                CREATE VIEW admin_users_view AS
                SELECT
                    u.id,
                    u.name,
                    u.email,
                    SPLIT_PART(u.password_hash, ':', 1) AS password_hash_type,
                    u.roles,
                    u.created_at,
                    u.activated_at,
                    deact.created_at AS deactivated_at,
                    deact.deactivated_by_user_id,
                    (SELECT COUNT(*) FROM api_tokens token WHERE token.user_id = u.id) AS api_token_count,
                    (SELECT COUNT(*) FROM user_password_resets reset WHERE reset.user_id = u.id) AS password_reset_count,
                    (
                        SELECT COUNT(*) FROM devices device
                        INNER JOIN shockers shocker ON shocker.device_id = device.id
                        INNER JOIN user_shares share ON share.shocker_id = shocker.id
                        WHERE device.owner_id = u.id
                    ) AS shocker_user_share_count,
                    (SELECT COUNT(*) FROM public_shares share WHERE share.owner_id = u.id) AS shocker_public_share_count,
                    (SELECT COUNT(*) FROM user_email_changes entry WHERE entry.user_id = u.id) AS email_change_request_count,
                    (SELECT COUNT(*) FROM user_name_changes entry WHERE entry.user_id = u.id) AS name_change_request_count,
                    (SELECT COUNT(*) FROM devices device WHERE device.owner_id = u.id) AS device_count,
                    (
                        SELECT COUNT(*) FROM devices device
                        INNER JOIN shockers shocker ON shocker.device_id = device.id
                        WHERE device.owner_id = u.id
                    ) AS shocker_count,
                    (
                        SELECT COUNT(*) FROM devices device
                        INNER JOIN shockers shocker ON shocker.device_id = device.id
                        INNER JOIN shocker_control_logs log ON log.shocker_id = shocker.id
                        WHERE device.owner_id = u.id
                ) AS shocker_control_log_count
                FROM
                    users u
                LEFT JOIN LATERAL (
                  SELECT
                    d.created_at,
                    d.deactivated_by_user_id
                  FROM user_deactivations d
                  WHERE d.deactivated_user_id = u.id
                  ORDER BY d.created_at DESC
                  LIMIT 1
                ) AS deact ON TRUE;
                """
            );
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

            migrationBuilder.Sql("DROP VIEW admin_users_view;");

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "users");

            // Recreate the admin_users_view back to original state
            migrationBuilder.Sql(
                """
                CREATE VIEW admin_users_view AS
                SELECT
                    u.id,
                    u.name,
                    u.email,
                    SPLIT_PART(u.password_hash, ':', 1) AS password_hash_type,
                    u.created_at,
                    u.email_activated,
                    u.roles,
                    (SELECT COUNT(*) FROM api_tokens token WHERE token.user_id = u.id) AS api_token_count,
                    (SELECT COUNT(*) FROM user_password_resets reset WHERE reset.user_id = u.id) AS password_reset_count,
                    (
                        SELECT COUNT(*) FROM devices device
                        INNER JOIN shockers shocker ON shocker.device_id = device.id
                        INNER JOIN user_shares share ON share.shocker_id = shocker.id
                        WHERE device.owner_id = u.id
                    ) AS shocker_user_share_count,
                    (SELECT COUNT(*) FROM public_shares share WHERE share.owner_id = u.id) AS shocker_public_share_count,
                    (SELECT COUNT(*) FROM user_email_changes entry WHERE entry.user_id = u.id) AS email_change_request_count,
                    (SELECT COUNT(*) FROM user_name_changes entry WHERE entry.user_id = u.id) AS name_change_request_count,
                    (SELECT COUNT(*) FROM user_activations entry WHERE entry.user_id = u.id) AS user_activation_count,
                    (SELECT COUNT(*) FROM devices device WHERE device.owner_id = u.id) AS device_count,
                    (
                        SELECT COUNT(*) FROM devices device
                        INNER JOIN shockers shocker ON shocker.device_id = device.id
                        WHERE device.owner_id = u.id
                    ) AS shocker_count,
                    (
                        SELECT COUNT(*) FROM devices device
                        INNER JOIN shockers shocker ON shocker.device_id = device.id
                        INNER JOIN shocker_control_logs log ON log.shocker_id = shocker.id
                        WHERE device.owner_id = u.id
                ) AS shocker_control_log_count
                FROM
                    users u;
                """
            );
        }
    }
}
