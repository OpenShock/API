using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenShock.Common.OpenShockDb;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <inheritdoc />
public partial class _20260930204951_AddAutomationTokens : Migration
{
    // Same as AddOAuthSupport's view, plus users.created_by_automation_token_id
    public const string Query_Create_AdminUsersView =
        """
        CREATE VIEW admin_users_view AS
            SELECT
                u.id,
                u.name,
                u.email,
                (CASE
                    WHEN u.password_hash IS NULL THEN NULL
                    ELSE SPLIT_PART(u.password_hash, ':', 1)
                END) AS password_hash_type,
                u.roles,
                u.created_at,
                u.activated_at,
                u.created_by_automation_token_id,
                deact.created_at AS deactivated_at,
                deact.deactivated_by_user_id,
                (SELECT COUNT(*) FROM api_tokens token WHERE token.user_id = u.id) AS api_token_count,
                (SELECT COUNT(*) FROM user_password_resets reset WHERE reset.user_id = u.id) AS password_reset_count,
                (
                    SELECT COUNT(*) FROM devices device
                    INNER JOIN shockers shocker ON shocker.device_id = device.id
                    INNER JOIN user_shares share ON share.shocker_id = shocker.id
                    WHERE device.owner_id = u.id
                ) AS shocker_user_share_count,
                (SELECT COUNT(*) FROM public_shares share WHERE share.owner_id = u.id) AS shocker_public_share_count,
                (SELECT COUNT(*) FROM user_email_changes entry WHERE entry.user_id = u.id) AS email_change_request_count,
                (SELECT COUNT(*) FROM user_name_changes entry WHERE entry.user_id = u.id) AS name_change_request_count,
                (SELECT COUNT(*) FROM devices device WHERE device.owner_id = u.id) AS device_count,
                (
                    SELECT COUNT(*) FROM devices device
                    INNER JOIN shockers shocker ON shocker.device_id = device.id
                    WHERE device.owner_id = u.id
                ) AS shocker_count,
                (
                    SELECT COUNT(*) FROM devices device
                    INNER JOIN shockers shocker ON shocker.device_id = device.id
                    INNER JOIN shocker_control_logs log ON log.shocker_id = shocker.id
                    WHERE device.owner_id = u.id
            ) AS shocker_control_log_count
            FROM
                users u
            LEFT JOIN LATERAL (
              SELECT
                d.created_at,
                d.deactivated_by_user_id
              FROM user_deactivations d
              WHERE d.deactivated_user_id = u.id
              ORDER BY d.created_at DESC
              LIMIT 1
            ) AS deact ON TRUE;
        """;

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(AddAdminUsersView.Query_Drop_AdminUsersView);

        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
            .Annotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted,automation_token_created,automation_token_updated,automation_token_rotated,automation_token_deleted,automation_token_used")
            .Annotation("Npgsql:Enum:automation_token_type", "turnstile,rate_limit")
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

        migrationBuilder.AddColumn<Guid>(
            name: "created_by_automation_token_id",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "automation_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, collation: "C"),
                types = table.Column<List<AutomationTokenType>>(type: "automation_token_type[]", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                last_rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                use_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                auto_cleanup_users = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                auto_cleanup_after = table.Column<TimeSpan>(type: "interval", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("automation_tokens_pkey", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_users_created_by_automation_token_id",
            table: "users",
            column: "created_by_automation_token_id");

        migrationBuilder.CreateIndex(
            name: "IX_automation_tokens_token_hash",
            table: "automation_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "fk_users_created_by_automation_token_id",
            table: "users",
            column: "created_by_automation_token_id",
            principalTable: "automation_tokens",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.Sql(Query_Create_AdminUsersView);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(AddAdminUsersView.Query_Drop_AdminUsersView);

        migrationBuilder.DropForeignKey(
            name: "fk_users_created_by_automation_token_id",
            table: "users");

        migrationBuilder.DropTable(
            name: "automation_tokens");

        migrationBuilder.DropIndex(
            name: "IX_users_created_by_automation_token_id",
            table: "users");

        migrationBuilder.DropColumn(
            name: "created_by_automation_token_id",
            table: "users");

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
            .OldAnnotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted,automation_token_created,automation_token_updated,automation_token_rotated,automation_token_deleted,automation_token_used")
            .OldAnnotation("Npgsql:Enum:automation_token_type", "turnstile,rate_limit")
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

        migrationBuilder.Sql(AddOAuthSupport.Query_Create_AdminUsersView);
    }
}
