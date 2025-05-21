using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantAnnotationAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_shares_shared_with_user_id",
                table: "user_shares");

            migrationBuilder.DropIndex(
                name: "IX_user_share_invites_owner_id",
                table: "user_share_invites");

            migrationBuilder.DropIndex(
                name: "IX_user_password_resets_user_id",
                table: "user_password_resets");

            migrationBuilder.DropIndex(
                name: "IX_user_name_changes_created_at",
                table: "user_name_changes");

            migrationBuilder.DropIndex(
                name: "IX_user_name_changes_old_name",
                table: "user_name_changes");

            migrationBuilder.DropIndex(
                name: "IX_user_name_changes_user_id",
                table: "user_name_changes");

            migrationBuilder.DropIndex(
                name: "IX_user_email_changes_created_at",
                table: "user_email_changes");

            migrationBuilder.DropIndex(
                name: "IX_user_email_changes_used_at",
                table: "user_email_changes");

            migrationBuilder.DropIndex(
                name: "IX_shockers_device_id",
                table: "shockers");

            migrationBuilder.DropIndex(
                name: "IX_shocker_control_logs_shocker_id",
                table: "shocker_control_logs");

            migrationBuilder.DropIndex(
                name: "IX_public_shares_owner_id",
                table: "public_shares");

            migrationBuilder.DropIndex(
                name: "IX_devices_owner_id",
                table: "devices");

            migrationBuilder.DropIndex(
                name: "device_ota_updates_created_at_idx",
                table: "device_ota_updates");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_user_id",
                table: "api_tokens");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_valid_until",
                table: "api_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_user_shares_shared_with_user_id",
                table: "user_shares",
                column: "shared_with_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_share_invites_owner_id",
                table: "user_share_invites",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_password_resets_user_id",
                table: "user_password_resets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_name_changes_created_at",
                table: "user_name_changes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_name_changes_old_name",
                table: "user_name_changes",
                column: "old_name");

            migrationBuilder.CreateIndex(
                name: "IX_user_name_changes_user_id",
                table: "user_name_changes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_email_changes_created_at",
                table: "user_email_changes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_email_changes_used_at",
                table: "user_email_changes",
                column: "used_at");

            migrationBuilder.CreateIndex(
                name: "IX_shockers_device_id",
                table: "shockers",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_control_logs_shocker_id",
                table: "shocker_control_logs",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_public_shares_owner_id",
                table: "public_shares",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_devices_owner_id",
                table: "devices",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "device_ota_updates_created_at_idx",
                table: "device_ota_updates",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_user_id",
                table: "api_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_valid_until",
                table: "api_tokens",
                column: "valid_until");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_shares_shared_with_user_id",
                table: "user_shares");

            migrationBuilder.DropIndex(
                name: "IX_user_share_invites_owner_id",
                table: "user_share_invites");

            migrationBuilder.DropIndex(
                name: "IX_user_password_resets_user_id",
                table: "user_password_resets");

            migrationBuilder.DropIndex(
                name: "IX_user_name_changes_created_at",
                table: "user_name_changes");

            migrationBuilder.DropIndex(
                name: "IX_user_name_changes_old_name",
                table: "user_name_changes");

            migrationBuilder.DropIndex(
                name: "IX_user_name_changes_user_id",
                table: "user_name_changes");

            migrationBuilder.DropIndex(
                name: "IX_user_email_changes_created_at",
                table: "user_email_changes");

            migrationBuilder.DropIndex(
                name: "IX_user_email_changes_used_at",
                table: "user_email_changes");

            migrationBuilder.DropIndex(
                name: "IX_shockers_device_id",
                table: "shockers");

            migrationBuilder.DropIndex(
                name: "IX_shocker_control_logs_shocker_id",
                table: "shocker_control_logs");

            migrationBuilder.DropIndex(
                name: "IX_public_shares_owner_id",
                table: "public_shares");

            migrationBuilder.DropIndex(
                name: "IX_devices_owner_id",
                table: "devices");

            migrationBuilder.DropIndex(
                name: "device_ota_updates_created_at_idx",
                table: "device_ota_updates");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_user_id",
                table: "api_tokens");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_valid_until",
                table: "api_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_user_shares_shared_with_user_id",
                table: "user_shares",
                column: "shared_with_user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_user_share_invites_owner_id",
                table: "user_share_invites",
                column: "owner_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_user_password_resets_user_id",
                table: "user_password_resets",
                column: "user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_user_name_changes_created_at",
                table: "user_name_changes",
                column: "created_at")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_user_name_changes_old_name",
                table: "user_name_changes",
                column: "old_name")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_user_name_changes_user_id",
                table: "user_name_changes",
                column: "user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_user_email_changes_created_at",
                table: "user_email_changes",
                column: "created_at")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_user_email_changes_used_at",
                table: "user_email_changes",
                column: "used_at")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_shockers_device_id",
                table: "shockers",
                column: "device_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_control_logs_shocker_id",
                table: "shocker_control_logs",
                column: "shocker_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_public_shares_owner_id",
                table: "public_shares",
                column: "owner_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_devices_owner_id",
                table: "devices",
                column: "owner_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "device_ota_updates_created_at_idx",
                table: "device_ota_updates",
                column: "created_at")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_user_id",
                table: "api_tokens",
                column: "user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_valid_until",
                table: "api_tokens",
                column: "valid_until")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");
        }
    }
}
