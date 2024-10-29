using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddAndOrgIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_name_changes_user_id",
                table: "users_name_changes");

            migrationBuilder.DropIndex(
                name: "idx_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_name",
                table: "users");

            migrationBuilder.DropIndex(
                name: "username",
                table: "users");

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
                name: "idx_user_name_changes_old_name",
                table: "users_name_changes",
                newName: "IX_users_name_changes_old_name");

            migrationBuilder.RenameIndex(
                name: "idx_user_name_changes_created_on",
                table: "users_name_changes",
                newName: "IX_users_name_changes_created_on");

            migrationBuilder.RenameIndex(
                name: "idx_email_changes_used_on",
                table: "users_email_changes",
                newName: "IX_users_email_changes_used_on");

            migrationBuilder.RenameIndex(
                name: "idx_email_changes_created_on",
                table: "users_email_changes",
                newName: "IX_users_email_changes_created_on");

            migrationBuilder.RenameIndex(
                name: "email",
                table: "users",
                newName: "IX_users_email");

            migrationBuilder.CreateIndex(
                name: "IX_users_name_changes_user_id",
                table: "users_name_changes",
                column: "user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_users_name",
                table: "users",
                column: "name",
                unique: true)
                .Annotation("Relational:Collation", new[] { "ndcoll" });

            migrationBuilder.CreateIndex(
                name: "IX_shockers_device",
                table: "shockers",
                column: "device")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_shares_links_owner_id",
                table: "shocker_shares_links",
                column: "owner_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_shares_shared_with",
                table: "shocker_shares",
                column: "shared_with")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_control_logs_shocker_id",
                table: "shocker_control_logs",
                column: "shocker_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_password_resets_user_id",
                table: "password_resets",
                column: "user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_devices_owner",
                table: "devices",
                column: "owner")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_devices_token",
                table: "devices",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_token",
                table: "api_tokens",
                column: "token",
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_name_changes_user_id",
                table: "users_name_changes");

            migrationBuilder.DropIndex(
                name: "IX_users_name",
                table: "users");

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
                name: "IX_devices_token",
                table: "devices");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_token",
                table: "api_tokens");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_user_id",
                table: "api_tokens");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_valid_until",
                table: "api_tokens");

            migrationBuilder.RenameIndex(
                name: "IX_users_name_changes_old_name",
                table: "users_name_changes",
                newName: "idx_user_name_changes_old_name");

            migrationBuilder.RenameIndex(
                name: "IX_users_name_changes_created_on",
                table: "users_name_changes",
                newName: "idx_user_name_changes_created_on");

            migrationBuilder.RenameIndex(
                name: "IX_users_email_changes_used_on",
                table: "users_email_changes",
                newName: "idx_email_changes_used_on");

            migrationBuilder.RenameIndex(
                name: "IX_users_email_changes_created_on",
                table: "users_email_changes",
                newName: "idx_email_changes_created_on");

            migrationBuilder.RenameIndex(
                name: "IX_users_email",
                table: "users",
                newName: "email");

            migrationBuilder.CreateIndex(
                name: "IX_users_name_changes_user_id",
                table: "users_name_changes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_email",
                table: "users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_name",
                table: "users",
                column: "name")
                .Annotation("Relational:Collation", new[] { "ndcoll" });

            migrationBuilder.CreateIndex(
                name: "username",
                table: "users",
                column: "name",
                unique: true);

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
