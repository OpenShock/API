using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class RenameShockerSharesToUserShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_share_request_shockers_share_request_id",
                table: "share_request_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_share_request_shockers_shocker_id",
                table: "share_request_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_owner_id",
                table: "share_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_user_id",
                table: "share_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_shares_shared_with_user_id",
                table: "shocker_shares");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_shares_shocker_id",
                table: "shocker_shares");

            migrationBuilder.DropPrimaryKey(
                name: "shocker_shares_pkey",
                table: "shocker_shares");

            migrationBuilder.DropPrimaryKey(
                name: "share_requests_pkey",
                table: "share_requests");

            migrationBuilder.DropPrimaryKey(
                name: "share_request_shockers_pkey",
                table: "share_request_shockers");

            migrationBuilder.RenameTable(
                name: "shocker_shares",
                newName: "user_shares");

            migrationBuilder.RenameTable(
                name: "share_requests",
                newName: "user_share_invites");

            migrationBuilder.RenameTable(
                name: "share_request_shockers",
                newName: "user_share_invite_shockers");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_shares_shared_with_user_id",
                table: "user_shares",
                newName: "IX_user_shares_shared_with_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_share_requests_user_id",
                table: "user_share_invites",
                newName: "IX_user_share_invites_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_share_requests_owner_id",
                table: "user_share_invites",
                newName: "IX_user_share_invites_owner_id");

            migrationBuilder.RenameColumn(
                name: "share_request_id",
                table: "user_share_invite_shockers",
                newName: "invite_id");

            migrationBuilder.RenameIndex(
                name: "IX_share_request_shockers_shocker_id",
                table: "user_share_invite_shockers",
                newName: "IX_user_share_invite_shockers_shocker_id");

            migrationBuilder.AddPrimaryKey(
                name: "user_shares_pkey",
                table: "user_shares",
                columns: new[] { "shared_with_user_id", "shocker_id" });

            migrationBuilder.AddPrimaryKey(
                name: "user_share_invites_pkey",
                table: "user_share_invites",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "user_share_invite_shockers_pkey",
                table: "user_share_invite_shockers",
                columns: new[] { "invite_id", "shocker_id" });

            migrationBuilder.CreateIndex(
                name: "IX_user_shares_shocker_id",
                table: "user_shares",
                column: "shocker_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_share_invite_shockers_invite_id",
                table: "user_share_invite_shockers",
                column: "invite_id",
                principalTable: "user_share_invites",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_share_invite_shockers_shocker_id",
                table: "user_share_invite_shockers",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_share_invites_owner_id",
                table: "user_share_invites",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_share_invites_recipient_user_id",
                table: "user_share_invites",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_shares_shared_with_user_id",
                table: "user_shares",
                column: "shared_with_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_shares_shocker_id",
                table: "user_shares",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Recreate the admin_users_view with modified names
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE VIEW admin_users_view AS
                WITH
                  api_tokens_count AS (
                    SELECT user_id, COUNT(*) AS cnt
                    FROM api_tokens
                    GROUP BY user_id
                  ),
                  password_resets_count AS (
                    SELECT user_id, COUNT(*) AS cnt
                    FROM user_password_resets
                    GROUP BY user_id
                  ),
                  user_shares_count AS (
                    SELECT dev.owner_id AS user_id, COUNT(*) AS cnt
                    FROM devices dev
                    INNER JOIN shockers sck    ON sck.device_id = dev.id
                    INNER JOIN user_shares ush ON ush.shocker_id = sck.id
                    GROUP BY dev.owner_id
                  ),
                  public_shares_count AS (
                    SELECT owner_id AS user_id, COUNT(*) AS cnt
                    FROM public_shares
                    GROUP BY owner_id
                  ),
                  email_changes_count AS (
                    SELECT user_id, COUNT(*) AS cnt
                    FROM users_email_changes
                    GROUP BY user_id
                  ),
                  name_changes_count AS (
                    SELECT user_id, COUNT(*) AS cnt
                    FROM users_name_changes
                    GROUP BY user_id
                  ),
                  activation_count AS (
                    SELECT user_id, COUNT(*) AS cnt
                    FROM users_activation
                    GROUP BY user_id
                  ),
                  device_count AS (
                    SELECT owner_id AS user_id, COUNT(*) AS cnt
                    FROM devices
                    GROUP BY owner_id
                  ),
                  shocker_count AS (
                    SELECT dev.owner_id AS user_id, COUNT(sck.id) AS cnt
                    FROM devices dev
                    INNER JOIN shockers sck ON sck.device_id = dev.id
                    GROUP BY dev.owner_id
                  ),
                  shocker_control_log_count AS (
                    SELECT dev.owner_id AS user_id, COUNT(scl.id) AS cnt
                    FROM devices dev
                    INNER JOIN shockers sck             ON sck.device_id = dev.id
                    INNER JOIN shocker_control_logs scl ON scl.shocker_id = sck.id
                    GROUP BY dev.owner_id
                  )
                SELECT
                  u.id,
                  u.name,
                  u.email,
                  SPLIT_PART(u.password_hash, ':', 1) AS password_hash_type,
                  u.created_at,
                  u.email_activated,
                  u.roles,
                  COALESCE(at.cnt, 0)  AS api_token_count,
                  COALESCE(pr.cnt, 0)  AS password_reset_count,
                  COALESCE(us.cnt, 0)  AS user_share_count,
                  COALESCE(ps.cnt, 0)  AS public_share_count,
                  COALESCE(ec.cnt, 0)  AS email_change_request_count,
                  COALESCE(nc.cnt, 0)  AS name_change_request_count,
                  COALESCE(ac.cnt, 0)  AS user_activation_count,
                  COALESCE(dc.cnt, 0)  AS device_count,
                  COALESCE(sc.cnt, 0)  AS shocker_count,
                  COALESCE(scl.cnt, 0) AS shocker_control_log_count
                FROM users u
                LEFT JOIN api_tokens_count          at ON at.user_id = u.id
                LEFT JOIN password_resets_count     pr ON pr.user_id = u.id
                LEFT JOIN user_shares_count         us ON us.user_id = u.id
                LEFT JOIN public_shares_count       ps ON ps.user_id = u.id
                LEFT JOIN email_changes_count       ec ON ec.user_id = u.id
                LEFT JOIN name_changes_count        nc ON nc.user_id = u.id
                LEFT JOIN activation_count          ac ON ac.user_id = u.id
                LEFT JOIN device_count              dc ON dc.user_id = u.id
                LEFT JOIN shocker_count             sc ON sc.user_id = u.id
                LEFT JOIN shocker_control_log_count sl ON sl.user_id = u.id;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_share_invite_shockers_invite_id",
                table: "user_share_invite_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_user_share_invite_shockers_shocker_id",
                table: "user_share_invite_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_user_share_invites_owner_id",
                table: "user_share_invites");

            migrationBuilder.DropForeignKey(
                name: "fk_user_share_invites_recipient_user_id",
                table: "user_share_invites");

            migrationBuilder.DropForeignKey(
                name: "fk_user_shares_shared_with_user_id",
                table: "user_shares");

            migrationBuilder.DropForeignKey(
                name: "fk_user_shares_shocker_id",
                table: "user_shares");

            migrationBuilder.DropPrimaryKey(
                name: "user_shares_pkey",
                table: "user_shares");

            migrationBuilder.DropIndex(
                name: "IX_user_shares_shocker_id",
                table: "user_shares");

            migrationBuilder.DropPrimaryKey(
                name: "user_share_invites_pkey",
                table: "user_share_invites");

            migrationBuilder.DropPrimaryKey(
                name: "user_share_invite_shockers_pkey",
                table: "user_share_invite_shockers");

            migrationBuilder.RenameTable(
                name: "user_shares",
                newName: "shocker_shares");

            migrationBuilder.RenameTable(
                name: "user_share_invites",
                newName: "share_requests");

            migrationBuilder.RenameTable(
                name: "user_share_invite_shockers",
                newName: "share_request_shockers");

            migrationBuilder.RenameIndex(
                name: "IX_user_shares_shared_with_user_id",
                table: "shocker_shares",
                newName: "IX_shocker_shares_shared_with_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_share_invites_user_id",
                table: "share_requests",
                newName: "IX_share_requests_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_share_invites_owner_id",
                table: "share_requests",
                newName: "IX_share_requests_owner_id");

            migrationBuilder.RenameColumn(
                name: "invite_id",
                table: "share_request_shockers",
                newName: "share_request_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_share_invite_shockers_shocker_id",
                table: "share_request_shockers",
                newName: "IX_share_request_shockers_shocker_id");

            migrationBuilder.AddPrimaryKey(
                name: "shocker_shares_pkey",
                table: "shocker_shares",
                columns: new[] { "shocker_id", "shared_with_user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "share_requests_pkey",
                table: "share_requests",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "share_request_shockers_pkey",
                table: "share_request_shockers",
                columns: new[] { "share_request_id", "shocker_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_share_request_shockers_share_request_id",
                table: "share_request_shockers",
                column: "share_request_id",
                principalTable: "share_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_share_request_shockers_shocker_id",
                table: "share_request_shockers",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_share_requests_owner_id",
                table: "share_requests",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_share_requests_user_id",
                table: "share_requests",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_shares_shared_with_user_id",
                table: "shocker_shares",
                column: "shared_with_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_shares_shocker_id",
                table: "shocker_shares",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Revert view back to old
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE VIEW admin_users_view AS
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
    }
}
