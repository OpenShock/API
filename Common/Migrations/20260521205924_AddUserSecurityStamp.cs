using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Any existing pending password-reset and email-change rows predate the security stamp,
            // so they have no valid snapshot to compare against once the column is added. Clear them
            // so affected users have to start a new flow; in-flight email links from before this
            // migration are dropped on the floor.
            migrationBuilder.Sql("DELETE FROM user_password_resets;");
            migrationBuilder.Sql("DELETE FROM user_email_changes;");

            migrationBuilder.AddColumn<Guid>(
                name: "security_stamp",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "security_stamp_at_create",
                table: "user_password_resets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "security_stamp_at_create",
                table: "user_email_changes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "security_stamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "security_stamp_at_create",
                table: "user_password_resets");

            migrationBuilder.DropColumn(
                name: "security_stamp_at_create",
                table: "user_email_changes");
        }
    }
}
