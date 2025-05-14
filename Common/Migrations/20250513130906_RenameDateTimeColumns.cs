using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class RenameDateTimeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "users_name_changes",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_users_name_changes_created_on",
                table: "users_name_changes",
                newName: "IX_users_name_changes_created_at");

            migrationBuilder.RenameColumn(
                name: "used_on",
                table: "users_email_changes",
                newName: "used_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "users_email_changes",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_users_email_changes_used_on",
                table: "users_email_changes",
                newName: "IX_users_email_changes_used_at");

            migrationBuilder.RenameIndex(
                name: "IX_users_email_changes_created_on",
                table: "users_email_changes",
                newName: "IX_users_email_changes_created_at");

            migrationBuilder.RenameColumn(
                name: "used_on",
                table: "users_activation",
                newName: "used_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "users_activation",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "shockers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "expires_on",
                table: "shocker_shares_links",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "shocker_shares_links",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "shocker_shares",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "shocker_share_codes",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "shocker_control_logs",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "share_requests",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "used_on",
                table: "password_resets",
                newName: "used_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "password_resets",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "devices",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "device_ota_updates",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "device_ota_updates_created_on_idx",
                table: "device_ota_updates",
                newName: "device_ota_updates_created_at_idx");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "api_tokens",
                newName: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "users_name_changes",
                newName: "created_on");

            migrationBuilder.RenameIndex(
                name: "IX_users_name_changes_created_at",
                table: "users_name_changes",
                newName: "IX_users_name_changes_created_on");

            migrationBuilder.RenameColumn(
                name: "used_at",
                table: "users_email_changes",
                newName: "used_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "users_email_changes",
                newName: "created_on");

            migrationBuilder.RenameIndex(
                name: "IX_users_email_changes_used_at",
                table: "users_email_changes",
                newName: "IX_users_email_changes_used_on");

            migrationBuilder.RenameIndex(
                name: "IX_users_email_changes_created_at",
                table: "users_email_changes",
                newName: "IX_users_email_changes_created_on");

            migrationBuilder.RenameColumn(
                name: "used_at",
                table: "users_activation",
                newName: "used_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "users_activation",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "shockers",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "shocker_shares_links",
                newName: "expires_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "shocker_shares_links",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "shocker_shares",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "shocker_share_codes",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "shocker_control_logs",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "share_requests",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "used_at",
                table: "password_resets",
                newName: "used_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "password_resets",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "devices",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "device_ota_updates",
                newName: "created_on");

            migrationBuilder.RenameIndex(
                name: "device_ota_updates_created_at_idx",
                table: "device_ota_updates",
                newName: "device_ota_updates_created_on_idx");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "api_tokens",
                newName: "created_on");
        }
    }
}
