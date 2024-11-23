using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class Typeandnamingcleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "email_actived",
                table: "users",
                newName: "email_activated");

            migrationBuilder.Sql(
                $"""
                 ALTER TABLE api_tokens ALTER COLUMN created_by_ip TYPE inet USING CAST(created_by_ip AS inet);
                 """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "email_activated",
                table: "users",
                newName: "email_actived");

            migrationBuilder.Sql(
                $"""
                 ALTER TABLE api_tokens ALTER COLUMN created_by_ip TYPE character varying(40) USING CAST(created_by_ip AS character varying(40));
                 """
            );
        }
    }
}
