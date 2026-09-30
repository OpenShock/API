using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenShock.Common.OpenShockDb;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <inheritdoc />
public partial class _20260930202003_AddBypassTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
            .Annotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted")
            .Annotation("Npgsql:Enum:bypass_token_type", "turnstile,rate_limit")
            .Annotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
            .Annotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
            .Annotation("Npgsql:Enum:control_type", "stop,shock,vibrate,sound")
            .Annotation("Npgsql:Enum:email_status", "pending,sending,sent,failed,skipped")
            .Annotation("Npgsql:Enum:email_type", "account_activation,password_reset,email_verification,email_change_notice")
            .Annotation("Npgsql:Enum:match_type_enum", "exact,contains")
            .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
            .Annotation("Npgsql:Enum:password_encryption_type", "bcrypt_enhanced,pbkdf2")
            .Annotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth,usershares.edit,usershares.pause,publicshares.pause,publicshares.edit")
            .Annotation("Npgsql:Enum:role_type", "support,staff,admin,system")
            .Annotation("Npgsql:Enum:shocker_model_type", "cai_xianlin,petrainer,petrainer_998dr,wellturn_t330")
            .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
            .OldAnnotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted")
            .OldAnnotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
            .OldAnnotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
            .OldAnnotation("Npgsql:Enum:control_type", "stop,shock,vibrate,sound")
            .OldAnnotation("Npgsql:Enum:email_status", "pending,sending,sent,failed,skipped")
            .OldAnnotation("Npgsql:Enum:email_type", "account_activation,password_reset,email_verification,email_change_notice")
            .OldAnnotation("Npgsql:Enum:match_type_enum", "exact,contains")
            .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
            .OldAnnotation("Npgsql:Enum:password_encryption_type", "bcrypt_enhanced,pbkdf2")
            .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth,usershares.edit,usershares.pause,publicshares.pause,publicshares.edit")
            .OldAnnotation("Npgsql:Enum:role_type", "support,staff,admin,system")
            .OldAnnotation("Npgsql:Enum:shocker_model_type", "cai_xianlin,petrainer,petrainer_998dr,wellturn_t330");

        migrationBuilder.CreateTable(
            name: "bypass_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, collation: "C"),
                types = table.Column<List<BypassTokenType>>(type: "bypass_token_type[]", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                last_used_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                use_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                auto_cleanup_users = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                auto_cleanup_after = table.Column<TimeSpan>(type: "interval", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("bypass_tokens_pkey", x => x.id);
                table.ForeignKey(
                    name: "fk_bypass_tokens_last_used_by_user_id",
                    column: x => x.last_used_by_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "bypass_token_user_uses",
            columns: table => new
            {
                bypass_token_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                first_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                use_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
            },
            constraints: table =>
            {
                table.PrimaryKey("bypass_token_user_uses_pkey", x => new { x.bypass_token_id, x.user_id });
                table.ForeignKey(
                    name: "fk_bypass_token_user_uses_bypass_token_id",
                    column: x => x.bypass_token_id,
                    principalTable: "bypass_tokens",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_bypass_token_user_uses_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bypass_token_user_uses_last_used_at",
            table: "bypass_token_user_uses",
            column: "last_used_at");

        migrationBuilder.CreateIndex(
            name: "IX_bypass_token_user_uses_user_id",
            table: "bypass_token_user_uses",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_bypass_tokens_last_used_by_user_id",
            table: "bypass_tokens",
            column: "last_used_by_user_id");

        migrationBuilder.CreateIndex(
            name: "IX_bypass_tokens_token_hash",
            table: "bypass_tokens",
            column: "token_hash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "bypass_token_user_uses");

        migrationBuilder.DropTable(
            name: "bypass_tokens");

        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
            .Annotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted")
            .Annotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
            .Annotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
            .Annotation("Npgsql:Enum:control_type", "stop,shock,vibrate,sound")
            .Annotation("Npgsql:Enum:email_status", "pending,sending,sent,failed,skipped")
            .Annotation("Npgsql:Enum:email_type", "account_activation,password_reset,email_verification,email_change_notice")
            .Annotation("Npgsql:Enum:match_type_enum", "exact,contains")
            .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
            .Annotation("Npgsql:Enum:password_encryption_type", "bcrypt_enhanced,pbkdf2")
            .Annotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth,usershares.edit,usershares.pause,publicshares.pause,publicshares.edit")
            .Annotation("Npgsql:Enum:role_type", "support,staff,admin,system")
            .Annotation("Npgsql:Enum:shocker_model_type", "cai_xianlin,petrainer,petrainer_998dr,wellturn_t330")
            .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
            .OldAnnotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted")
            .OldAnnotation("Npgsql:Enum:bypass_token_type", "turnstile,rate_limit")
            .OldAnnotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
            .OldAnnotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
            .OldAnnotation("Npgsql:Enum:control_type", "stop,shock,vibrate,sound")
            .OldAnnotation("Npgsql:Enum:email_status", "pending,sending,sent,failed,skipped")
            .OldAnnotation("Npgsql:Enum:email_type", "account_activation,password_reset,email_verification,email_change_notice")
            .OldAnnotation("Npgsql:Enum:match_type_enum", "exact,contains")
            .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
            .OldAnnotation("Npgsql:Enum:password_encryption_type", "bcrypt_enhanced,pbkdf2")
            .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth,usershares.edit,usershares.pause,publicshares.pause,publicshares.edit")
            .OldAnnotation("Npgsql:Enum:role_type", "support,staff,admin,system")
            .OldAnnotation("Npgsql:Enum:shocker_model_type", "cai_xianlin,petrainer,petrainer_998dr,wellturn_t330");
    }
}
