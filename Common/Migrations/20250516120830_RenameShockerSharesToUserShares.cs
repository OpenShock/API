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
                DROP VIEW admin_users_view;
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
