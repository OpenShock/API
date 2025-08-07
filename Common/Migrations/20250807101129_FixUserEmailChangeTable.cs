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

            // Fix all non-activated accounts
            migrationBuilder.Sql("""
                UPDATE users
                SET activated_at = created_at
                WHERE activated_at IS NULL
                  AND NOT EXISTS (
                    SELECT 1
                    FROM user_deactivations AS d
                    WHERE d.deactivated_user_id = users.id
                  )
                  AND NOT EXISTS (
                    SELECT 1
                    FROM user_activation_requests AS r
                    WHERE r.user_id = users.id
                  );
                """);
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
