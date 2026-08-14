using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class MakeApiTokenLastUsedNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "last_used",
                table: "api_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "'-infinity'::timestamp without time zone");

            // Tokens that were never used were stored with the '-infinity' sentinel; represent that as NULL now.
            migrationBuilder.Sql("UPDATE api_tokens SET last_used = NULL WHERE last_used = '-infinity'::timestamptz;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the '-infinity' sentinel before reinstating the NOT NULL constraint.
            migrationBuilder.Sql("UPDATE api_tokens SET last_used = '-infinity'::timestamptz WHERE last_used IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_used",
                table: "api_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "'-infinity'::timestamp without time zone",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
