using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class RenameShareLinksToPublicShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shocker_share_link_shockers_share_link_id",
                table: "shocker_share_link_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_share_link_shockers_shocker_id",
                table: "shocker_share_link_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_shocker_share_links_owner_id",
                table: "shocker_share_links");

            migrationBuilder.DropPrimaryKey(
                name: "shocker_share_links_pkey",
                table: "shocker_share_links");

            migrationBuilder.DropPrimaryKey(
                name: "shocker_share_link_shockers_pkey",
                table: "shocker_share_link_shockers");

            migrationBuilder.RenameTable(
                name: "shocker_share_links",
                newName: "public_shares");

            migrationBuilder.RenameTable(
                name: "shocker_share_link_shockers",
                newName: "public_share_shockers");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_share_links_owner_id",
                table: "public_shares",
                newName: "IX_public_shares_owner_id");

            migrationBuilder.RenameColumn(
                name: "share_link_id",
                table: "public_share_shockers",
                newName: "public_share_id");

            migrationBuilder.RenameIndex(
                name: "IX_shocker_share_link_shockers_shocker_id",
                table: "public_share_shockers",
                newName: "IX_public_share_shockers_shocker_id");

            migrationBuilder.AddPrimaryKey(
                name: "public_shares_pkey",
                table: "public_shares",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "public_share_shockers_pkey",
                table: "public_share_shockers",
                columns: new[] { "public_share_id", "shocker_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_public_share_shockers_public_share_id",
                table: "public_share_shockers",
                column: "public_share_id",
                principalTable: "public_shares",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_public_share_shockers_shocker_id",
                table: "public_share_shockers",
                column: "shocker_id",
                principalTable: "shockers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_public_shares_owner_id",
                table: "public_shares",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_public_share_shockers_public_share_id",
                table: "public_share_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_public_share_shockers_shocker_id",
                table: "public_share_shockers");

            migrationBuilder.DropForeignKey(
                name: "fk_public_shares_owner_id",
                table: "public_shares");

            migrationBuilder.DropPrimaryKey(
                name: "public_shares_pkey",
                table: "public_shares");

            migrationBuilder.DropPrimaryKey(
                name: "public_share_shockers_pkey",
                table: "public_share_shockers");

            migrationBuilder.RenameTable(
                name: "public_shares",
                newName: "shocker_share_links");

            migrationBuilder.RenameTable(
                name: "public_share_shockers",
                newName: "shocker_share_link_shockers");

            migrationBuilder.RenameIndex(
                name: "IX_public_shares_owner_id",
                table: "shocker_share_links",
                newName: "IX_shocker_share_links_owner_id");

            migrationBuilder.RenameColumn(
                name: "public_share_id",
                table: "shocker_share_link_shockers",
                newName: "share_link_id");

            migrationBuilder.RenameIndex(
                name: "IX_public_share_shockers_shocker_id",
                table: "shocker_share_link_shockers",
                newName: "IX_shocker_share_link_shockers_shocker_id");

            migrationBuilder.AddPrimaryKey(
                name: "shocker_share_links_pkey",
                table: "shocker_share_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "shocker_share_link_shockers_pkey",
                table: "shocker_share_link_shockers",
                columns: new[] { "share_link_id", "shocker_id" });

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
        }
    }
}
