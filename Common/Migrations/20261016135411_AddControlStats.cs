using System;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenShock.Common.OpenShockDb;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <inheritdoc />
public partial class AddControlStats : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
            .Annotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted,automation_token_created,automation_token_updated,automation_token_rotated,automation_token_deleted,automation_token_used")
            .Annotation("Npgsql:Enum:automation_token_type", "turnstile,rate_limit,control_stats_read")
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

        migrationBuilder.CreateTable(
            name: "control_stat_batches",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("control_stat_batches_pkey", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "control_stats",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                shocker_id = table.Column<Guid>(type: "uuid", nullable: true),
                controller_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                bucket = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                type = table.Column<ControlType>(type: "control_type", nullable: false),
                live = table.Column<bool>(type: "boolean", nullable: false),
                count = table.Column<long>(type: "bigint", nullable: false),
                duration_ms = table.Column<long>(type: "bigint", nullable: false),
                intensity_ms = table.Column<long>(type: "bigint", nullable: false),
                max_intensity = table.Column<byte>(type: "smallint", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("control_stats_pkey", x => x.id);
                table.ForeignKey(
                    name: "fk_control_stats_owner_user_id",
                    column: x => x.owner_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_control_stats_shocker_id",
                    column: x => x.shocker_id,
                    principalTable: "shockers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_control_stat_batches_applied_at",
            table: "control_stat_batches",
            column: "applied_at");

        migrationBuilder.CreateIndex(
            name: "IX_control_stats_controller_user_id_bucket",
            table: "control_stats",
            columns: new[] { "controller_user_id", "bucket" });

        migrationBuilder.CreateIndex(
            name: "IX_control_stats_owner_user_id_bucket",
            table: "control_stats",
            columns: new[] { "owner_user_id", "bucket" });

        migrationBuilder.CreateIndex(
            name: "IX_control_stats_shocker_id",
            table: "control_stats",
            column: "shocker_id");

        migrationBuilder.CreateIndex(
            name: "IX_control_stats_updated_at",
            table: "control_stats",
            column: "updated_at");

        migrationBuilder.CreateIndex(
            name: "ix_control_stats_upsert_key",
            table: "control_stats",
            columns: new[] { "owner_user_id", "shocker_id", "controller_user_id", "bucket", "type", "live" },
            unique: true,
            filter: "shocker_id IS NOT NULL")
            .Annotation("Npgsql:NullsDistinct", false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "control_stat_batches");

        migrationBuilder.DropTable(
            name: "control_stats");

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
            .OldAnnotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted,automation_token_created,automation_token_updated,automation_token_rotated,automation_token_deleted,automation_token_used")
            .OldAnnotation("Npgsql:Enum:automation_token_type", "turnstile,rate_limit,control_stats_read")
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
