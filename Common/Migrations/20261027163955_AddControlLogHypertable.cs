using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <inheritdoc />
public partial class AddControlLogHypertable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_shocker_control_logs_controlled_by_user_id",
            table: "shocker_control_logs");

        migrationBuilder.DropForeignKey(
            name: "fk_shocker_control_logs_shocker_id",
            table: "shocker_control_logs");

        migrationBuilder.DropPrimaryKey(
            name: "shocker_control_logs_pkey",
            table: "shocker_control_logs");

        migrationBuilder.DropIndex(
            name: "IX_shocker_control_logs_shocker_id",
            table: "shocker_control_logs");

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
            .Annotation("Npgsql:PostgresExtension:timescaledb", ",,")
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

        migrationBuilder.Sql(
            """
            ALTER TABLE shocker_control_logs
                ADD COLUMN owner_user_id uuid,
                ADD COLUMN max_intensity smallint,
                ADD COLUMN inserted_at timestamp with time zone;

            UPDATE shocker_control_logs l
            SET owner_user_id = d.owner_id,
                max_intensity = l.intensity,
                inserted_at = l.created_at
            FROM shockers s
            JOIN devices d ON d.id = s.device_id
            WHERE s.id = l.shocker_id;

            ALTER TABLE shocker_control_logs
                ALTER COLUMN owner_user_id SET NOT NULL,
                ALTER COLUMN max_intensity SET NOT NULL,
                ALTER COLUMN inserted_at SET DEFAULT now(),
                ALTER COLUMN inserted_at SET NOT NULL;
            """);

        migrationBuilder.AddPrimaryKey(
            name: "shocker_control_logs_pkey",
            table: "shocker_control_logs",
            columns: new[] { "id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_shocker_control_logs_owner_user_id_created_at",
            table: "shocker_control_logs",
            columns: new[] { "owner_user_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_shocker_control_logs_shocker_id_created_at",
            table: "shocker_control_logs",
            columns: new[] { "shocker_id", "created_at" });

        // Segmented by owner, every read and the owner delete below filter by it
        migrationBuilder.Sql(
            """
            SELECT create_hypertable('shocker_control_logs', by_range('created_at', INTERVAL '7 days'), migrate_data => true);

            ALTER TABLE shocker_control_logs SET (
                timescaledb.enable_columnstore = true,
                timescaledb.segmentby = 'owner_user_id',
                timescaledb.orderby = 'created_at DESC');

            CALL add_columnstore_policy('shocker_control_logs', after => INTERVAL '7 days');
            SELECT add_retention_policy('shocker_control_logs', drop_after => INTERVAL '1 year');
            """);

        // Logs have no foreign keys, but the owner's logs go with their account
        migrationBuilder.Sql(
            """
            CREATE FUNCTION users_delete_shocker_control_logs() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                DELETE FROM shocker_control_logs l USING removed r WHERE l.owner_user_id = r.id;
                RETURN NULL;
            END
            $$;

            CREATE TRIGGER users_delete_shocker_control_logs
                AFTER DELETE ON users
                REFERENCING OLD TABLE AS removed
                FOR EACH STATEMENT EXECUTE FUNCTION users_delete_shocker_control_logs();
            """);

        // 15 minute buckets, every time zone's UTC offset is a multiple of that. The refresh window must stay well
        // within the logs' retention, refreshing a bucket whose logs were dropped would wipe its stats.
        migrationBuilder.Sql(
            """
            CREATE MATERIALIZED VIEW control_stats
            WITH (timescaledb.continuous, timescaledb.materialized_only = false) AS
            SELECT time_bucket(INTERVAL '15 minutes', created_at) AS bucket,
                   owner_user_id,
                   shocker_id,
                   controlled_by_user_id AS controller_user_id,
                   type,
                   live_control AS live,
                   count(*) AS count,
                   sum(duration)::bigint AS duration_ms,
                   sum(intensity::bigint * duration)::bigint AS intensity_ms,
                   max(max_intensity) AS max_intensity
            FROM shocker_control_logs
            WHERE type <> 'stop'
            GROUP BY bucket, owner_user_id, shocker_id, controlled_by_user_id, type, live_control
            WITH NO DATA;

            SELECT add_continuous_aggregate_policy('control_stats',
                start_offset => INTERVAL '3 days',
                end_offset => INTERVAL '1 hour',
                schedule_interval => INTERVAL '5 minutes');
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("shocker_control_logs can't be turned back into a plain table, restore a backup instead");
    }
}
