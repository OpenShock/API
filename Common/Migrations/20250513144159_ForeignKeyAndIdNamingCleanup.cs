using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class ForeignKeyAndIdNamingCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_id",
                table: "api_tokens");

            migrationBuilder.DropForeignKey(
                name: "device_ota_updates_device",
                table: "device_ota_updates");

            migrationBuilder.DropForeignKey(
                name: "owner_user_id",
                table: "devices");

            migrationBuilder.DropForeignKey(
                name: "user_id",
                table: "password_resets");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_owner",
                table: "share_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_user",
                table: "share_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_shockers_share_request",
                table: "share_requests_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_shockers_shocker",
                table: "share_requests_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_controlled_by",
                table: "shocker_control_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_id",
                table: "shocker_control_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_id",
                table: "shocker_share_codes");

            migrationBuilder.DropForeignKey(
                name: "ref_shocker_id",
                table: "shocker_shares");

            migrationBuilder.DropForeignKey(
                name: "shared_with_user_id",
                table: "shocker_shares");

            migrationBuilder.DropForeignKey(
                name: "owner_id",
                table: "shocker_shares_links");

            migrationBuilder.DropForeignKey(
                name: "share_link_id",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropForeignKey(
                name: "shocker_id",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropForeignKey(
                name: "device_id",
                table: "shockers");

            migrationBuilder.DropForeignKey(
                name: "user_id",
                table: "users_activation");

            migrationBuilder.DropForeignKey(
                name: "fk_user_id",
                table: "users_email_changes");

            migrationBuilder.DropForeignKey(
                name: "fk_user_id",
                table: "users_name_changes");

            migrationBuilder.RenameColumn(
                name: "device",
                table: "shockers",
                newName: "device_id");

            migrationBuilder.RenameIndex(
                name: "IX_shockers_device",
                table: "shockers",
                newName: "IX_shockers_device_id");

            migrationBuilder.RenameColumn(
                name: "shared_with",
                table: "shocker_shares",
                newName: "shared_with_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_shares_shared_with",
                table: "shocker_shares",
                newName: "IX_shocker_shares_shared_with_user_id");

            migrationBuilder.RenameColumn(
                name: "controlled_by",
                table: "shocker_control_logs",
                newName: "controlled_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_control_logs_controlled_by",
                table: "shocker_control_logs",
                newName: "IX_shocker_control_logs_controlled_by_user_id");

            migrationBuilder.RenameColumn(
                name: "shocker",
                table: "share_requests_shockers",
                newName: "shocker_id");

            migrationBuilder.RenameColumn(
                name: "share_request",
                table: "share_requests_shockers",
                newName: "share_request_id");

            migrationBuilder.RenameIndex(
                name: "IX_share_requests_shockers_shocker",
                table: "share_requests_shockers",
                newName: "IX_share_requests_shockers_shocker_id");

            migrationBuilder.RenameColumn(
                name: "user",
                table: "share_requests",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "owner",
                table: "share_requests",
                newName: "owner_id");

            migrationBuilder.RenameIndex(
                name: "IX_share_requests_user",
                table: "share_requests",
                newName: "IX_share_requests_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_share_requests_owner",
                table: "share_requests",
                newName: "IX_share_requests_owner_id");

            migrationBuilder.RenameColumn(
                name: "owner",
                table: "devices",
                newName: "owner_id");

            migrationBuilder.RenameIndex(
                name: "IX_devices_owner",
                table: "devices",
                newName: "IX_devices_owner_id");

            migrationBuilder.RenameColumn(
                name: "device",
                table: "device_ota_updates",
                newName: "device_id");

            migrationBuilder.AddForeignKey(
                name: "fk_api_tokens_user_id",
                table: "api_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_device_ota_updates_device_id",
                table: "device_ota_updates",
                column: "device_id",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_devices_owner_id",
                table: "devices",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_password_resets_user_id",
                table: "password_resets",
                column: "user_id",
                principalTable: "users",
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
                name: "fk_share_requests_shockers_share_request_id",
                table: "share_requests_shockers",
                column: "share_request_id",
                principalTable: "share_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_share_requests_shockers_shocker_id",
                table: "share_requests_shockers",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_control_logs_controlled_by_user_id",
                table: "shocker_control_logs",
                column: "controlled_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_control_logs_shocker_id",
                table: "shocker_control_logs",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_share_codes_shocker_id",
                table: "shocker_share_codes",
                column: "shocker_id",
                principalTable: "shockers",
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

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_shares_links_owner_id",
                table: "shocker_shares_links",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_shares_links_shockers_share_link_id",
                table: "shocker_shares_links_shockers",
                column: "share_link_id",
                principalTable: "shocker_shares_links",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_shares_links_shockers_shocker_id",
                table: "shocker_shares_links_shockers",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shockers_device_id",
                table: "shockers",
                column: "device_id",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_users_activation_user_id",
                table: "users_activation",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_users_email_changes_user_id",
                table: "users_email_changes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_users_name_changes_user_id",
                table: "users_name_changes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_api_tokens_user_id",
                table: "api_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_device_ota_updates_device_id",
                table: "device_ota_updates");

            migrationBuilder.DropForeignKey(
                name: "fk_devices_owner_id",
                table: "devices");

            migrationBuilder.DropForeignKey(
                name: "fk_password_resets_user_id",
                table: "password_resets");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_owner_id",
                table: "share_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_user_id",
                table: "share_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_shockers_share_request_id",
                table: "share_requests_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_shockers_shocker_id",
                table: "share_requests_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_control_logs_controlled_by_user_id",
                table: "shocker_control_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_control_logs_shocker_id",
                table: "shocker_control_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_share_codes_shocker_id",
                table: "shocker_share_codes");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_shares_shared_with_user_id",
                table: "shocker_shares");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_shares_shocker_id",
                table: "shocker_shares");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_shares_links_owner_id",
                table: "shocker_shares_links");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_shares_links_shockers_share_link_id",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_shares_links_shockers_shocker_id",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_shockers_device_id",
                table: "shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_users_activation_user_id",
                table: "users_activation");

            migrationBuilder.DropForeignKey(
                name: "fk_users_email_changes_user_id",
                table: "users_email_changes");

            migrationBuilder.DropForeignKey(
                name: "fk_users_name_changes_user_id",
                table: "users_name_changes");

            migrationBuilder.RenameColumn(
                name: "device_id",
                table: "shockers",
                newName: "device");

            migrationBuilder.RenameIndex(
                name: "IX_shockers_device_id",
                table: "shockers",
                newName: "IX_shockers_device");

            migrationBuilder.RenameColumn(
                name: "shared_with_user_id",
                table: "shocker_shares",
                newName: "shared_with");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_shares_shared_with_user_id",
                table: "shocker_shares",
                newName: "IX_shocker_shares_shared_with");

            migrationBuilder.RenameColumn(
                name: "controlled_by_user_id",
                table: "shocker_control_logs",
                newName: "controlled_by");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_control_logs_controlled_by_user_id",
                table: "shocker_control_logs",
                newName: "IX_shocker_control_logs_controlled_by");

            migrationBuilder.RenameColumn(
                name: "shocker_id",
                table: "share_requests_shockers",
                newName: "shocker");

            migrationBuilder.RenameColumn(
                name: "share_request_id",
                table: "share_requests_shockers",
                newName: "share_request");

            migrationBuilder.RenameIndex(
                name: "IX_share_requests_shockers_shocker_id",
                table: "share_requests_shockers",
                newName: "IX_share_requests_shockers_shocker");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "share_requests",
                newName: "user");

            migrationBuilder.RenameColumn(
                name: "owner_id",
                table: "share_requests",
                newName: "owner");

            migrationBuilder.RenameIndex(
                name: "IX_share_requests_user_id",
                table: "share_requests",
                newName: "IX_share_requests_user");

            migrationBuilder.RenameIndex(
                name: "IX_share_requests_owner_id",
                table: "share_requests",
                newName: "IX_share_requests_owner");

            migrationBuilder.RenameColumn(
                name: "owner_id",
                table: "devices",
                newName: "owner");

            migrationBuilder.RenameIndex(
                name: "IX_devices_owner_id",
                table: "devices",
                newName: "IX_devices_owner");

            migrationBuilder.RenameColumn(
                name: "device_id",
                table: "device_ota_updates",
                newName: "device");

            migrationBuilder.AddForeignKey(
                name: "fk_user_id",
                table: "api_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "device_ota_updates_device",
                table: "device_ota_updates",
                column: "device",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "owner_user_id",
                table: "devices",
                column: "owner",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "user_id",
                table: "password_resets",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_share_requests_owner",
                table: "share_requests",
                column: "owner",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_share_requests_user",
                table: "share_requests",
                column: "user",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_share_requests_shockers_share_request",
                table: "share_requests_shockers",
                column: "share_request",
                principalTable: "share_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_share_requests_shockers_shocker",
                table: "share_requests_shockers",
                column: "shocker",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_controlled_by",
                table: "shocker_control_logs",
                column: "controlled_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_id",
                table: "shocker_control_logs",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_id",
                table: "shocker_share_codes",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "ref_shocker_id",
                table: "shocker_shares",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "shared_with_user_id",
                table: "shocker_shares",
                column: "shared_with",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "owner_id",
                table: "shocker_shares_links",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "share_link_id",
                table: "shocker_shares_links_shockers",
                column: "share_link_id",
                principalTable: "shocker_shares_links",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "shocker_id",
                table: "shocker_shares_links_shockers",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "device_id",
                table: "shockers",
                column: "device",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "user_id",
                table: "users_activation",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_id",
                table: "users_email_changes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_id",
                table: "users_name_changes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
