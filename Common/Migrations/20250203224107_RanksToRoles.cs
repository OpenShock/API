using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class RanksToRoles : Migration
    {
        public const string Query_Create_AdminUsersView =
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
            """;
        
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the view temporarily to modify the underlying table
            migrationBuilder.Sql(AddAdminUsersView.Query_Drop_AdminUsersView);
            
            migrationBuilder.Sql(
                """
                -- Add the roles column as a text array to replace the rank enum
                ALTER TABLE users ADD roles text[] NOT NULL DEFAULT ARRAY[]::text[];
                
                -- Migrate existing rank values into the roles array column
                UPDATE users
                SET roles = (
                    CASE
                        WHEN rank = 'support' THEN ARRAY['support']
                        WHEN rank = 'staff'   THEN ARRAY['staff']
                        WHEN rank = 'admin'   THEN ARRAY['admin']
                        WHEN rank = 'system'  THEN ARRAY['system']
                        ELSE ARRAY[]::text[]
                    END
                );
                
                -- Remove the rank column after migration
                ALTER TABLE users DROP COLUMN rank;
                
                -- Replace the old rank_type enum with a new role_type enum, dropping 'user' in the process
                DROP TYPE rank_type;
                CREATE TYPE role_type AS ENUM ('support', 'staff', 'admin', 'system');
                
                -- Update the roles column to use the new role_type enum array
                ALTER TABLE users ALTER COLUMN roles SET DEFAULT ARRAY[]::role_type[];
                ALTER TABLE users ALTER COLUMN roles TYPE role_type[] USING CAST(roles as role_type[]);
                """
            );
            
            // Recreate the admin_users_view to reflect the new roles structure
            migrationBuilder.Sql(Query_Create_AdminUsersView);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the view temporarily to modify the underlying table
            migrationBuilder.Sql(AddAdminUsersView.Query_Drop_AdminUsersView);
            
            migrationBuilder.Sql(
                """
                -- Add the rank column back as a temporary nullable text column
                ALTER TABLE users ADD rank text;
                
                -- Migrate roles array values back into a single rank value
                UPDATE users
                SET rank = (
                    CASE
                        WHEN 'system'  = ANY(roles) THEN 'system'
                        WHEN 'admin'   = ANY(roles) THEN 'admin'
                        WHEN 'staff'   = ANY(roles) THEN 'staff'
                        WHEN 'support' = ANY(roles) THEN 'support'
                        ELSE 'user'
                    END
                );
                
                -- Remove the roles column after migration
                ALTER TABLE users DROP COLUMN roles;
                
                -- Restore the old rank_type enum
                DROP TYPE role_type;
                CREATE TYPE rank_type AS ENUM ('user', 'support', 'staff', 'admin', 'system');
                
                -- Change the rank column back to a non-nullable rank_type enum
                ALTER TABLE users ALTER COLUMN rank TYPE rank_type USING CAST(rank as rank_type);
                ALTER TABLE users ALTER COLUMN rank SET NOT NULL;
                """
            );
            
            // Recreate the admin_users_view to restore the original structure
            migrationBuilder.Sql(AddAdminUsersView.Query_Create_AdminUsersView);
        }
    }
}
