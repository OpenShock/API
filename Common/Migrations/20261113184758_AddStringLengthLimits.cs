using Microsoft.EntityFrameworkCore.Migrations;
using OpenShock.Common.OpenShockDb;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <inheritdoc />
public partial class _20261113184758_AddStringLengthLimits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The columns below were unbounded text. A truncated credential is useless, so abort with a clear
        // error if any existing webhook token exceeds the new limit.
        migrationBuilder.Sql(MigrationQueries.FailIfLonger("discord_webhooks", "webhook_token", 255));

        // Truncate anything else longer than the new limit first, otherwise the type change fails on existing rows.
        migrationBuilder.Sql("UPDATE user_oauth_connections SET display_name = left(display_name, 64) WHERE char_length(display_name) > 64;");

        migrationBuilder.AlterColumn<string>(
            name: "display_name",
            table: "user_oauth_connections",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.Sql("UPDATE user_oauth_connections SET external_id = left(external_id, 255) WHERE char_length(external_id) > 255;");

        migrationBuilder.AlterColumn<string>(
            name: "external_id",
            table: "user_oauth_connections",
            type: "character varying(255)",
            maxLength: 255,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.Sql("UPDATE user_oauth_connections SET provider_key = left(provider_key, 16) WHERE char_length(provider_key) > 16;");

        migrationBuilder.AlterColumn<string>(
            name: "provider_key",
            table: "user_oauth_connections",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            collation: "C",
            oldClrType: typeof(string),
            oldType: "text",
            oldCollation: "C");

        migrationBuilder.AlterColumn<string>(
            name: "webhook_token",
            table: "discord_webhooks",
            type: "character varying(255)",
            maxLength: 255,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.Sql("UPDATE discord_webhooks SET name = left(name, 64) WHERE char_length(name) > 64;");

        migrationBuilder.AlterColumn<string>(
            name: "name",
            table: "discord_webhooks",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.Sql("UPDATE configuration SET value = left(value, 4096) WHERE char_length(value) > 4096;");

        migrationBuilder.AlterColumn<string>(
            name: "value",
            table: "configuration",
            type: "character varying(4096)",
            maxLength: 4096,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.Sql("UPDATE configuration SET description = left(description, 255) WHERE char_length(description) > 255;");

        migrationBuilder.AlterColumn<string>(
            name: "description",
            table: "configuration",
            type: "character varying(255)",
            maxLength: 255,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.Sql("UPDATE configuration SET name = left(name, 64) WHERE char_length(name) > 64;");

        migrationBuilder.AlterColumn<string>(
            name: "name",
            table: "configuration",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            collation: "C",
            oldClrType: typeof(string),
            oldType: "text",
            oldCollation: "C");

        migrationBuilder.Sql("UPDATE api_token_reports SET ip_country = left(ip_country, 4) WHERE char_length(ip_country) > 4;");

        migrationBuilder.AlterColumn<string>(
            name: "ip_country",
            table: "api_token_reports",
            type: "character varying(4)",
            maxLength: 4,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "display_name",
            table: "user_oauth_connections",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "external_id",
            table: "user_oauth_connections",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(255)",
            oldMaxLength: 255);

        migrationBuilder.AlterColumn<string>(
            name: "provider_key",
            table: "user_oauth_connections",
            type: "text",
            nullable: false,
            collation: "C",
            oldClrType: typeof(string),
            oldType: "character varying(16)",
            oldMaxLength: 16,
            oldCollation: "C");

        migrationBuilder.AlterColumn<string>(
            name: "webhook_token",
            table: "discord_webhooks",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(255)",
            oldMaxLength: 255);

        migrationBuilder.AlterColumn<string>(
            name: "name",
            table: "discord_webhooks",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64);

        migrationBuilder.AlterColumn<string>(
            name: "value",
            table: "configuration",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(4096)",
            oldMaxLength: 4096);

        migrationBuilder.AlterColumn<string>(
            name: "description",
            table: "configuration",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(255)",
            oldMaxLength: 255);

        migrationBuilder.AlterColumn<string>(
            name: "name",
            table: "configuration",
            type: "text",
            nullable: false,
            collation: "C",
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64,
            oldCollation: "C");

        migrationBuilder.AlterColumn<string>(
            name: "ip_country",
            table: "api_token_reports",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(4)",
            oldMaxLength: 4,
            oldNullable: true);
    }
}
