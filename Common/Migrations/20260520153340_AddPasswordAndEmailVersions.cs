using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordAndEmailVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "email_version",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "password_version",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "password_version_at_create",
                table: "user_password_resets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "email_version_at_create",
                table: "user_email_changes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_version",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_version",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_version_at_create",
                table: "user_password_resets");

            migrationBuilder.DropColumn(
                name: "email_version_at_create",
                table: "user_email_changes");
        }
    }
}
