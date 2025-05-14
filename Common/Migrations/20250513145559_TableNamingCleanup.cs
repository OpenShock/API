using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class TableNamingCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_password_resets_user_id",
                table: "password_resets");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_shockers_share_request_id",
                table: "share_requests_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_share_requests_shockers_shocker_id",
                table: "share_requests_shockers");

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
                name: "fk_users_activation_user_id",
                table: "users_activation");

            migrationBuilder.DropForeignKey(
                name: "fk_users_email_changes_user_id",
                table: "users_email_changes");

            migrationBuilder.DropForeignKey(
                name: "fk_users_name_changes_user_id",
                table: "users_name_changes");

            migrationBuilder.DropPrimaryKey(
                name: "shares_codes_pkey",
                table: "share_requests");

            migrationBuilder.DropPrimaryKey(
                name: "users_name_changes_pkey",
                table: "users_name_changes");

            migrationBuilder.DropPrimaryKey(
                name: "users_email_change_pkey",
                table: "users_email_changes");

            migrationBuilder.DropPrimaryKey(
                name: "users_activation_pkey",
                table: "users_activation");

            migrationBuilder.DropPrimaryKey(
                name: "shocker_shares_links_shockers_pkey",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropPrimaryKey(
                name: "shocker_shares_links_pkey",
                table: "shocker_shares_links");

            migrationBuilder.DropPrimaryKey(
                name: "share_requests_shockers_pkey",
                table: "share_requests_shockers");

            migrationBuilder.DropPrimaryKey(
                name: "password_resets_pkey",
                table: "password_resets");

            migrationBuilder.RenameTable(
                name: "users_name_changes",
                newName: "user_name_changes");

            migrationBuilder.RenameTable(
                name: "users_email_changes",
                newName: "user_email_changes");

            migrationBuilder.RenameTable(
                name: "users_activation",
                newName: "user_activations");

            migrationBuilder.RenameTable(
                name: "shocker_shares_links_shockers",
                newName: "shocker_share_link_shockers");

            migrationBuilder.RenameTable(
                name: "shocker_shares_links",
                newName: "shocker_share_links");

            migrationBuilder.RenameTable(
                name: "share_requests_shockers",
                newName: "share_request_shockers");

            migrationBuilder.RenameTable(
                name: "password_resets",
                newName: "user_password_resets");

            migrationBuilder.RenameIndex(
                name: "IX_users_name_changes_user_id",
                table: "user_name_changes",
                newName: "IX_user_name_changes_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_users_name_changes_old_name",
                table: "user_name_changes",
                newName: "IX_user_name_changes_old_name");

            migrationBuilder.RenameIndex(
                name: "IX_users_name_changes_created_at",
                table: "user_name_changes",
                newName: "IX_user_name_changes_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_users_email_changes_user_id",
                table: "user_email_changes",
                newName: "IX_user_email_changes_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_users_email_changes_used_at",
                table: "user_email_changes",
                newName: "IX_user_email_changes_used_at");

            migrationBuilder.RenameIndex(
                name: "IX_users_email_changes_created_at",
                table: "user_email_changes",
                newName: "IX_user_email_changes_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_users_activation_user_id",
                table: "user_activations",
                newName: "IX_user_activations_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_shares_links_shockers_shocker_id",
                table: "shocker_share_link_shockers",
                newName: "IX_shocker_share_link_shockers_shocker_id");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_shares_links_owner_id",
                table: "shocker_share_links",
                newName: "IX_shocker_share_links_owner_id");

            migrationBuilder.RenameIndex(
                name: "IX_share_requests_shockers_shocker_id",
                table: "share_request_shockers",
                newName: "IX_share_request_shockers_shocker_id");

            migrationBuilder.RenameIndex(
                name: "IX_password_resets_user_id",
                table: "user_password_resets",
                newName: "IX_user_password_resets_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "share_requests_pkey",
                table: "share_requests",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "user_name_changes_pkey",
                table: "user_name_changes",
                columns: new[] { "id", "user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "user_email_changes_pkey",
                table: "user_email_changes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "user_activations_pkey",
                table: "user_activations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "shocker_share_link_shockers_pkey",
                table: "shocker_share_link_shockers",
                columns: new[] { "share_link_id", "shocker_id" });

            migrationBuilder.AddPrimaryKey(
                name: "shocker_share_links_pkey",
                table: "shocker_share_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "share_request_shockers_pkey",
                table: "share_request_shockers",
                columns: new[] { "share_request_id", "shocker_id" });

            migrationBuilder.AddPrimaryKey(
                name: "user_password_resets_pkey",
                table: "user_password_resets",
                column: "id");

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
                name: "fk_shocker_share_link_shockers_share_link_id",
                table: "shocker_share_link_shockers",
                column: "share_link_id",
                principalTable: "shocker_share_links",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_share_link_shockers_shocker_id",
                table: "shocker_share_link_shockers",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shocker_share_links_owner_id",
                table: "shocker_share_links",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_activations_user_id",
                table: "user_activations",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_email_changes_user_id",
                table: "user_email_changes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_name_changes_user_id",
                table: "user_name_changes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_password_resets_user_id",
                table: "user_password_resets",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_share_request_shockers_share_request_id",
                table: "share_request_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_share_request_shockers_shocker_id",
                table: "share_request_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_share_link_shockers_share_link_id",
                table: "shocker_share_link_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_share_link_shockers_shocker_id",
                table: "shocker_share_link_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_share_links_owner_id",
                table: "shocker_share_links");

            migrationBuilder.DropForeignKey(
                name: "fk_user_activations_user_id",
                table: "user_activations");

            migrationBuilder.DropForeignKey(
                name: "fk_user_email_changes_user_id",
                table: "user_email_changes");

            migrationBuilder.DropForeignKey(
                name: "fk_user_name_changes_user_id",
                table: "user_name_changes");

            migrationBuilder.DropForeignKey(
                name: "fk_user_password_resets_user_id",
                table: "user_password_resets");

            migrationBuilder.DropPrimaryKey(
                name: "share_requests_pkey",
                table: "share_requests");

            migrationBuilder.DropPrimaryKey(
                name: "user_password_resets_pkey",
                table: "user_password_resets");

            migrationBuilder.DropPrimaryKey(
                name: "user_name_changes_pkey",
                table: "user_name_changes");

            migrationBuilder.DropPrimaryKey(
                name: "user_email_changes_pkey",
                table: "user_email_changes");

            migrationBuilder.DropPrimaryKey(
                name: "user_activations_pkey",
                table: "user_activations");

            migrationBuilder.DropPrimaryKey(
                name: "shocker_share_links_pkey",
                table: "shocker_share_links");

            migrationBuilder.DropPrimaryKey(
                name: "shocker_share_link_shockers_pkey",
                table: "shocker_share_link_shockers");

            migrationBuilder.DropPrimaryKey(
                name: "share_request_shockers_pkey",
                table: "share_request_shockers");

            migrationBuilder.RenameTable(
                name: "user_password_resets",
                newName: "password_resets");

            migrationBuilder.RenameTable(
                name: "user_name_changes",
                newName: "users_name_changes");

            migrationBuilder.RenameTable(
                name: "user_email_changes",
                newName: "users_email_changes");

            migrationBuilder.RenameTable(
                name: "user_activations",
                newName: "users_activation");

            migrationBuilder.RenameTable(
                name: "shocker_share_links",
                newName: "shocker_shares_links");

            migrationBuilder.RenameTable(
                name: "shocker_share_link_shockers",
                newName: "shocker_shares_links_shockers");

            migrationBuilder.RenameTable(
                name: "share_request_shockers",
                newName: "share_requests_shockers");

            migrationBuilder.RenameIndex(
                name: "IX_user_password_resets_user_id",
                table: "password_resets",
                newName: "IX_password_resets_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_name_changes_user_id",
                table: "users_name_changes",
                newName: "IX_users_name_changes_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_name_changes_old_name",
                table: "users_name_changes",
                newName: "IX_users_name_changes_old_name");

            migrationBuilder.RenameIndex(
                name: "IX_user_name_changes_created_at",
                table: "users_name_changes",
                newName: "IX_users_name_changes_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_user_email_changes_user_id",
                table: "users_email_changes",
                newName: "IX_users_email_changes_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_email_changes_used_at",
                table: "users_email_changes",
                newName: "IX_users_email_changes_used_at");

            migrationBuilder.RenameIndex(
                name: "IX_user_email_changes_created_at",
                table: "users_email_changes",
                newName: "IX_users_email_changes_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_user_activations_user_id",
                table: "users_activation",
                newName: "IX_users_activation_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_share_links_owner_id",
                table: "shocker_shares_links",
                newName: "IX_shocker_shares_links_owner_id");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_share_link_shockers_shocker_id",
                table: "shocker_shares_links_shockers",
                newName: "IX_shocker_shares_links_shockers_shocker_id");

            migrationBuilder.RenameIndex(
                name: "IX_share_request_shockers_shocker_id",
                table: "share_requests_shockers",
                newName: "IX_share_requests_shockers_shocker_id");

            migrationBuilder.AddPrimaryKey(
                name: "shares_codes_pkey",
                table: "share_requests",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "password_resets_pkey",
                table: "password_resets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "users_name_changes_pkey",
                table: "users_name_changes",
                columns: new[] { "id", "user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "users_email_change_pkey",
                table: "users_email_changes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "users_activation_pkey",
                table: "users_activation",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "shocker_shares_links_pkey",
                table: "shocker_shares_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "shocker_shares_links_shockers_pkey",
                table: "shocker_shares_links_shockers",
                columns: new[] { "share_link_id", "shocker_id" });

            migrationBuilder.AddPrimaryKey(
                name: "share_requests_shockers_pkey",
                table: "share_requests_shockers",
                columns: new[] { "share_request_id", "shocker_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_password_resets_user_id",
                table: "password_resets",
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
    }
}
