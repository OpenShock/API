using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateSafetySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "perm_shock",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropColumn(
                name: "perm_sound",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropColumn(
                name: "perm_vibrate",
                table: "shocker_shares_links_shockers");

            migrationBuilder.RenameColumn(
                name: "paused",
                table: "shockers",
                newName: "is_paused");

            migrationBuilder.RenameColumn(
                name: "perm_live",
                table: "shocker_shares_links_shockers",
                newName: "allow_vibrate");

            migrationBuilder.RenameColumn(
                name: "paused",
                table: "shocker_shares_links_shockers",
                newName: "is_paused");

            migrationBuilder.RenameColumn(
                name: "limit_intensity",
                table: "shocker_shares_links_shockers",
                newName: "max_intensity");

            migrationBuilder.RenameColumn(
                name: "limit_duration",
                table: "shocker_shares_links_shockers",
                newName: "max_duration");

            migrationBuilder.RenameColumn(
                name: "perm_vibrate",
                table: "shocker_shares",
                newName: "allow_vibrate");

            migrationBuilder.RenameColumn(
                name: "perm_sound",
                table: "shocker_shares",
                newName: "allow_sound");

            migrationBuilder.RenameColumn(
                name: "perm_shock",
                table: "shocker_shares",
                newName: "allow_shock");

            migrationBuilder.RenameColumn(
                name: "perm_live",
                table: "shocker_shares",
                newName: "allow_livecontrol");

            migrationBuilder.RenameColumn(
                name: "paused",
                table: "shocker_shares",
                newName: "is_paused");

            migrationBuilder.RenameColumn(
                name: "limit_intensity",
                table: "shocker_shares",
                newName: "max_intensity");

            migrationBuilder.RenameColumn(
                name: "limit_duration",
                table: "shocker_shares",
                newName: "max_duration");

            migrationBuilder.RenameColumn(
                name: "perm_vibrate",
                table: "shocker_share_codes",
                newName: "allow_vibrate");

            migrationBuilder.RenameColumn(
                name: "perm_sound",
                table: "shocker_share_codes",
                newName: "allow_sound");

            migrationBuilder.RenameColumn(
                name: "perm_shock",
                table: "shocker_share_codes",
                newName: "allow_shock");

            migrationBuilder.RenameColumn(
                name: "limit_intensity",
                table: "shocker_share_codes",
                newName: "max_intensity");

            migrationBuilder.RenameColumn(
                name: "limit_duration",
                table: "shocker_share_codes",
                newName: "max_duration");

            migrationBuilder.RenameColumn(
                name: "perm_vibrate",
                table: "share_requests_shockers",
                newName: "allow_vibrate");

            migrationBuilder.RenameColumn(
                name: "perm_sound",
                table: "share_requests_shockers",
                newName: "allow_sound");

            migrationBuilder.RenameColumn(
                name: "perm_shock",
                table: "share_requests_shockers",
                newName: "allow_shock");

            migrationBuilder.RenameColumn(
                name: "perm_live",
                table: "share_requests_shockers",
                newName: "allow_livecontrol");

            migrationBuilder.RenameColumn(
                name: "limit_intensity",
                table: "share_requests_shockers",
                newName: "max_intensity");

            migrationBuilder.RenameColumn(
                name: "limit_duration",
                table: "share_requests_shockers",
                newName: "max_duration");

            migrationBuilder.AddColumn<bool>(
                name: "allow_livecontrol",
                table: "shocker_shares_links_shockers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_shock",
                table: "shocker_shares_links_shockers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_sound",
                table: "shocker_shares_links_shockers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_livecontrol",
                table: "shocker_share_codes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_paused",
                table: "shocker_share_codes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_paused",
                table: "share_requests_shockers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_livecontrol",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropColumn(
                name: "allow_shock",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropColumn(
                name: "allow_sound",
                table: "shocker_shares_links_shockers");

            migrationBuilder.DropColumn(
                name: "allow_livecontrol",
                table: "shocker_share_codes");

            migrationBuilder.DropColumn(
                name: "is_paused",
                table: "shocker_share_codes");

            migrationBuilder.DropColumn(
                name: "is_paused",
                table: "share_requests_shockers");

            migrationBuilder.RenameColumn(
                name: "is_paused",
                table: "shockers",
                newName: "paused");

            migrationBuilder.RenameColumn(
                name: "max_intensity",
                table: "shocker_shares_links_shockers",
                newName: "limit_intensity");

            migrationBuilder.RenameColumn(
                name: "max_duration",
                table: "shocker_shares_links_shockers",
                newName: "limit_duration");

            migrationBuilder.RenameColumn(
                name: "is_paused",
                table: "shocker_shares_links_shockers",
                newName: "paused");

            migrationBuilder.RenameColumn(
                name: "allow_vibrate",
                table: "shocker_shares_links_shockers",
                newName: "perm_live");

            migrationBuilder.RenameColumn(
                name: "max_intensity",
                table: "shocker_shares",
                newName: "limit_intensity");

            migrationBuilder.RenameColumn(
                name: "max_duration",
                table: "shocker_shares",
                newName: "limit_duration");

            migrationBuilder.RenameColumn(
                name: "is_paused",
                table: "shocker_shares",
                newName: "paused");

            migrationBuilder.RenameColumn(
                name: "allow_vibrate",
                table: "shocker_shares",
                newName: "perm_vibrate");

            migrationBuilder.RenameColumn(
                name: "allow_sound",
                table: "shocker_shares",
                newName: "perm_sound");

            migrationBuilder.RenameColumn(
                name: "allow_shock",
                table: "shocker_shares",
                newName: "perm_shock");

            migrationBuilder.RenameColumn(
                name: "allow_livecontrol",
                table: "shocker_shares",
                newName: "perm_live");

            migrationBuilder.RenameColumn(
                name: "max_intensity",
                table: "shocker_share_codes",
                newName: "limit_intensity");

            migrationBuilder.RenameColumn(
                name: "max_duration",
                table: "shocker_share_codes",
                newName: "limit_duration");

            migrationBuilder.RenameColumn(
                name: "allow_vibrate",
                table: "shocker_share_codes",
                newName: "perm_vibrate");

            migrationBuilder.RenameColumn(
                name: "allow_sound",
                table: "shocker_share_codes",
                newName: "perm_sound");

            migrationBuilder.RenameColumn(
                name: "allow_shock",
                table: "shocker_share_codes",
                newName: "perm_shock");

            migrationBuilder.RenameColumn(
                name: "max_intensity",
                table: "share_requests_shockers",
                newName: "limit_intensity");

            migrationBuilder.RenameColumn(
                name: "max_duration",
                table: "share_requests_shockers",
                newName: "limit_duration");

            migrationBuilder.RenameColumn(
                name: "allow_vibrate",
                table: "share_requests_shockers",
                newName: "perm_vibrate");

            migrationBuilder.RenameColumn(
                name: "allow_sound",
                table: "share_requests_shockers",
                newName: "perm_sound");

            migrationBuilder.RenameColumn(
                name: "allow_shock",
                table: "share_requests_shockers",
                newName: "perm_shock");

            migrationBuilder.RenameColumn(
                name: "allow_livecontrol",
                table: "share_requests_shockers",
                newName: "perm_live");

            migrationBuilder.AddColumn<bool>(
                name: "perm_shock",
                table: "shocker_shares_links_shockers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "perm_sound",
                table: "shocker_shares_links_shockers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "perm_vibrate",
                table: "shocker_shares_links_shockers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
