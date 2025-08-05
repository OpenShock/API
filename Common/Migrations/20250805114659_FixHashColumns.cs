using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class FixHashColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "secret",
                table: "user_password_resets",
                newName: "token_hash");

            migrationBuilder.RenameColumn(
                name: "secret",
                table: "user_email_changes",
                newName: "token_hash");

            migrationBuilder.RenameColumn(
                name: "secret",
                table: "user_activation_requests",
                newName: "token_hash");

            migrationBuilder.Sql("DROP VIEW admin_users_view;");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

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

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "user_password_resets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "user_email_changes",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "user_activation_requests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "token",
                table: "devices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "api_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_user_activation_requests_token_hash",
                table: "user_activation_requests",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_activation_requests_token_hash",
                table: "user_activation_requests");

            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "user_password_resets",
                newName: "secret");

            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "user_email_changes",
                newName: "secret");

            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "user_activation_requests",
                newName: "secret");

            migrationBuilder.Sql("DROP VIEW admin_users_view;");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldCollation: "C");

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

            migrationBuilder.AlterColumn<string>(
                name: "secret",
                table: "user_password_resets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldCollation: "C");

            migrationBuilder.AlterColumn<string>(
                name: "secret",
                table: "user_email_changes",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldCollation: "C");

            migrationBuilder.AlterColumn<string>(
                name: "secret",
                table: "user_activation_requests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldCollation: "C");

            migrationBuilder.AlterColumn<string>(
                name: "token",
                table: "devices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldCollation: "C");

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                table: "api_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldCollation: "C");
        }
    }
}
