using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminUsersView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS(SELECT *
                    FROM information_schema.columns
                    WHERE table_name='admin_users_view' and column_name='email_actived')
                THEN
                    ALTER VIEW admin_users_view RENAME COLUMN email_actived TO email_activated;
                END IF;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER VIEW admin_users_view RENAME COLUMN email_activated TO email_actived");
        }
    }
}
