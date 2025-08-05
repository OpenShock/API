using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class FixUserEmailChangeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "email",
                table: "user_email_changes",
                newName: "email_new");

            migrationBuilder.AddColumn<string>(
                name: "email_old",
                table: "user_email_changes",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_old",
                table: "user_email_changes");

            migrationBuilder.RenameColumn(
                name: "email_new",
                table: "user_email_changes",
                newName: "email");
        }
    }
}
