using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUsersView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE VIEW admin_users_view AS
                    SELECT
                        u.id,
                        u.name,
                        u.email,
                        SPLIT_PART(u.password_hash, ':', 1) AS password_hash_type,
                        u.created_at,
                        u.email_actived,
                        u.rank,
                        (SELECT COUNT(*) FROM api_tokens           ato WHERE ato.user_id     = u.id) AS api_token_count,
                        (SELECT COUNT(*) FROM password_resets      pre WHERE pre.user_id     = u.id) AS password_reset_count,
                        (SELECT COUNT(*) FROM shocker_shares       ssh WHERE ssh.shared_with = u.id) AS shocker_share_count,
                        (SELECT COUNT(*) FROM shocker_shares_links ssl WHERE ssl.owner_id    = u.id) AS shocker_share_link_count,
                        (SELECT COUNT(*) FROM users_email_changes  uec WHERE uec.user_id     = u.id) AS email_change_request_count,
                        (SELECT COUNT(*) FROM users_name_changes   unc WHERE unc.user_id     = u.id) AS name_change_request_count,
                        (SELECT COUNT(*) FROM users_activation     uac WHERE uac.user_id     = u.id) AS user_activation_count,
                        (SELECT COUNT(*) FROM devices              dev WHERE dev.owner       = u.id) AS device_count,
                        (SELECT COUNT(*) FROM devices              dev WHERE dev.owner       = u.id JOIN shockers sck ON dev.id = sck.device) AS shocker_count,
                        (SELECT COUNT(*) FROM devices              dev WHERE dev.owner       = u.id JOIN shockers sck ON dev.id = sck.device JOIN shocker_control_logs scl ON scl.shocker_id = sck.id) AS shocker_control_log_count
                    FROM
                        users u;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW admin_users_view");
        }
    }
}
