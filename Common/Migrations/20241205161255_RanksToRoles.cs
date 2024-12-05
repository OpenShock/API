using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class RanksToRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // We need to drop the view to modify the target table
            migrationBuilder.Sql(
                """
                DROP VIEW admin_users_view;
                ALTER TYPE rank_type RENAME TO role_type;
                """
            );
            
            migrationBuilder.AddColumn<int[]>(
                name: "roles",
                table: "users",
                type: "role_type[]",
                nullable: false,
                defaultValue: Array.Empty<int>());

            // Migrate data from 'rank' to 'roles'
            migrationBuilder.Sql(
                """
                UPDATE users
                SET roles = CAST((
                    CASE
                        WHEN rank = 'user' THEN ARRAY['user']
                        WHEN rank = 'support' THEN ARRAY['user', 'support']
                        WHEN rank = 'staff'   THEN ARRAY['user', 'support', 'staff']
                        WHEN rank = 'admin'   THEN ARRAY['user', 'support', 'staff', 'admin']
                        WHEN rank = 'system'  THEN ARRAY['user', 'support', 'staff', 'admin', 'system']
                        ELSE CAST(ARRAY[] AS text[])
                    END
                ) AS role_type[]);
                """
            );
            
            migrationBuilder.DropColumn(
                name: "rank",
                table: "users");
            
            // Re-Create the view
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
                        (SELECT COUNT(*) FROM api_tokens           ato WHERE ato.user_id     = u.id) AS api_token_count,
                        (SELECT COUNT(*) FROM password_resets      pre WHERE pre.user_id     = u.id) AS password_reset_count,
                        (SELECT COUNT(*) FROM shocker_shares       ssh WHERE ssh.shared_with = u.id) AS shocker_share_count,
                        (SELECT COUNT(*) FROM shocker_shares_links ssl WHERE ssl.owner_id    = u.id) AS shocker_share_link_count,
                        (SELECT COUNT(*) FROM users_email_changes  uec WHERE uec.user_id     = u.id) AS email_change_request_count,
                        (SELECT COUNT(*) FROM users_name_changes   unc WHERE unc.user_id     = u.id) AS name_change_request_count,
                        (SELECT COUNT(*) FROM users_activation     uac WHERE uac.user_id     = u.id) AS user_activation_count,
                        (SELECT COUNT(*) FROM devices              dev WHERE dev.owner       = u.id) AS device_count,
                        (SELECT COUNT(*) FROM devices              dev JOIN shockers sck ON dev.id = sck.device WHERE dev.owner = u.id) AS shocker_count,
                        (SELECT COUNT(*) FROM devices              dev JOIN shockers sck ON dev.id = sck.device JOIN shocker_control_logs scl ON scl.shocker_id = sck.id WHERE dev.owner = u.id) AS shocker_control_log_count
                    FROM
                        users u;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // We need to drop the view to modify the target table
            migrationBuilder.Sql(
                """
                DROP VIEW admin_users_view;
                ALTER TYPE role_type RENAME TO rank_type;
                """
            );
            
            migrationBuilder.AddColumn<int>(
                name: "rank",
                table: "users",
                type: "rank_type",
                nullable: true);
            
            migrationBuilder.Sql(
                """
                UPDATE users
                SET rank = CAST((
                    CASE
                        WHEN 'system'  = ANY(roles) THEN 'system'
                        WHEN 'admin'   = ANY(roles) THEN 'admin'
                        WHEN 'staff'   = ANY(roles) THEN 'staff'
                        WHEN 'support' = ANY(roles) THEN 'support'
                        ELSE 'user'
                    END
                ) AS rank_type);
                """
            );

            migrationBuilder.AlterColumn<int>(
                name: "rank",
                table: "users",
                nullable: false);

            migrationBuilder.DropColumn(
                name: "roles",
                table: "users");
            
            // Re-Create the view
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
                        u.rank,
                        (SELECT COUNT(*) FROM api_tokens           ato WHERE ato.user_id     = u.id) AS api_token_count,
                        (SELECT COUNT(*) FROM password_resets      pre WHERE pre.user_id     = u.id) AS password_reset_count,
                        (SELECT COUNT(*) FROM shocker_shares       ssh WHERE ssh.shared_with = u.id) AS shocker_share_count,
                        (SELECT COUNT(*) FROM shocker_shares_links ssl WHERE ssl.owner_id    = u.id) AS shocker_share_link_count,
                        (SELECT COUNT(*) FROM users_email_changes  uec WHERE uec.user_id     = u.id) AS email_change_request_count,
                        (SELECT COUNT(*) FROM users_name_changes   unc WHERE unc.user_id     = u.id) AS name_change_request_count,
                        (SELECT COUNT(*) FROM users_activation     uac WHERE uac.user_id     = u.id) AS user_activation_count,
                        (SELECT COUNT(*) FROM devices              dev WHERE dev.owner       = u.id) AS device_count,
                        (SELECT COUNT(*) FROM devices              dev JOIN shockers sck ON dev.id = sck.device WHERE dev.owner = u.id) AS shocker_count,
                        (SELECT COUNT(*) FROM devices              dev JOIN shockers sck ON dev.id = sck.device JOIN shocker_control_logs scl ON scl.shocker_id = sck.id WHERE dev.owner = u.id) AS shocker_control_log_count
                    FROM
                        users u;
                """
            );
        }
    }
}
