using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddALotOfIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_name_changes_user_id",
                table: "users_name_changes");

            migrationBuilder.DropIndex(
                name: "IX_shockers_device",
                table: "shockers");

            migrationBuilder.DropIndex(
                name: "IX_shocker_shares_links_owner_id",
                table: "shocker_shares_links");

            migrationBuilder.DropIndex(
                name: "IX_shocker_shares_shared_with",
                table: "shocker_shares");

            migrationBuilder.DropIndex(
                name: "IX_shocker_control_logs_shocker_id",
                table: "shocker_control_logs");

            migrationBuilder.DropIndex(
                name: "IX_password_resets_user_id",
                table: "password_resets");

            migrationBuilder.DropIndex(
                name: "IX_devices_owner",
                table: "devices");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_user_id",
                table: "api_tokens");

            migrationBuilder.RenameIndex(
                name: "username",
                table: "users",
                newName: "name");

            migrationBuilder.CreateIndex(
                name: "idx_user_name_changes_user",
                table: "users_name_changes",
                column: "user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_shockers_device",
                table: "shockers",
                column: "device")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_shocker_shares_links_owner",
                table: "shocker_shares_links",
                column: "owner_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_shocker_shares_shared_with",
                table: "shocker_shares",
                column: "shared_with")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_shocker_control_logs_shocker",
                table: "shocker_control_logs",
                column: "shocker_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_password_resets_user",
                table: "password_resets",
                column: "user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_devices_owner",
                table: "devices",
                column: "owner")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "unique_devices_token",
                table: "devices",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_api_tokens_user",
                table: "api_tokens",
                column: "user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_api_tokens_valid_until",
                table: "api_tokens",
                column: "valid_until")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_user_name_changes_user",
                table: "users_name_changes");

            migrationBuilder.DropIndex(
                name: "idx_shockers_device",
                table: "shockers");

            migrationBuilder.DropIndex(
                name: "idx_shocker_shares_links_owner",
                table: "shocker_shares_links");

            migrationBuilder.DropIndex(
                name: "idx_shocker_shares_shared_with",
                table: "shocker_shares");

            migrationBuilder.DropIndex(
                name: "idx_shocker_control_logs_shocker",
                table: "shocker_control_logs");

            migrationBuilder.DropIndex(
                name: "idx_password_resets_user",
                table: "password_resets");

            migrationBuilder.DropIndex(
                name: "idx_devices_owner",
                table: "devices");

            migrationBuilder.DropIndex(
                name: "unique_devices_token",
                table: "devices");

            migrationBuilder.DropIndex(
                name: "idx_api_tokens_user",
                table: "api_tokens");

            migrationBuilder.DropIndex(
                name: "idx_api_tokens_valid_until",
                table: "api_tokens");

            migrationBuilder.RenameIndex(
                name: "name",
                table: "users",
                newName: "username");

            migrationBuilder.CreateIndex(
                name: "IX_users_name_changes_user_id",
                table: "users_name_changes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shockers_device",
                table: "shockers",
                column: "device");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_shares_links_owner_id",
                table: "shocker_shares_links",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_shares_shared_with",
                table: "shocker_shares",
                column: "shared_with");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_control_logs_shocker_id",
                table: "shocker_control_logs",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_password_resets_user_id",
                table: "password_resets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_devices_owner",
                table: "devices",
                column: "owner");

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_user_id",
                table: "api_tokens",
                column: "user_id");
        }
    }
}
