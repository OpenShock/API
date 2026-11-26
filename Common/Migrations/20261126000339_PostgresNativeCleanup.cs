using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations;

/// <inheritdoc />
public partial class _20261126000339_PostgresNativeCleanup : Migration
{
    // Same as AddAutomationTokens' view, but the control logs are counted by their owner. That is an index
    // lookup instead of a join through every device and shocker, and it keeps counting the logs of deleted shockers.
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
                (SELECT COUNT(*) FROM shocker_control_logs log WHERE log.owner_user_id = u.id) AS shocker_control_log_count
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

    /// <summary>
    /// Brings limits written before they were validated into range, so the check constraints can be added.
    /// Controls were already clamped to these bounds when sent, the stored value just says so now.
    /// </summary>
    private const string Query_ClampLimits =
        """
        UPDATE user_shares SET max_intensity = LEAST(GREATEST(max_intensity, 0), 100) WHERE max_intensity NOT BETWEEN 0 AND 100;
        UPDATE user_shares SET max_duration = LEAST(GREATEST(max_duration, 300), 65535) WHERE max_duration NOT BETWEEN 300 AND 65535;
        UPDATE user_share_invite_shockers SET max_intensity = LEAST(GREATEST(max_intensity, 0), 100) WHERE max_intensity NOT BETWEEN 0 AND 100;
        UPDATE user_share_invite_shockers SET max_duration = LEAST(GREATEST(max_duration, 300), 65535) WHERE max_duration NOT BETWEEN 300 AND 65535;
        UPDATE shocker_share_codes SET max_intensity = LEAST(GREATEST(max_intensity, 0), 100) WHERE max_intensity NOT BETWEEN 0 AND 100;
        UPDATE shocker_share_codes SET max_duration = LEAST(GREATEST(max_duration, 300), 65535) WHERE max_duration NOT BETWEEN 300 AND 65535;
        UPDATE public_share_shockers SET max_intensity = LEAST(GREATEST(max_intensity, 0), 100) WHERE max_intensity NOT BETWEEN 0 AND 100;
        UPDATE public_share_shockers SET max_duration = LEAST(GREATEST(max_duration, 300), 65535) WHERE max_duration NOT BETWEEN 300 AND 65535;
        UPDATE public_share_shockers SET cooldown = 0 WHERE cooldown < 0;
        """;

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The view selects users.email, whose collation can't change while the view depends on it
        migrationBuilder.Sql(AddAdminUsersView.Query_Drop_AdminUsersView);

        migrationBuilder.Sql(Query_ClampLimits);

        migrationBuilder.DropIndex(
            name: "IX_users_email",
            table: "users");

        migrationBuilder.DropIndex(
            name: "IX_user_shares_shared_with_user_id",
            table: "user_shares");

        migrationBuilder.DropPrimaryKey(
            name: "user_name_changes_pkey",
            table: "user_name_changes");

        migrationBuilder.DropIndex(
            name: "IX_user_name_changes_created_at",
            table: "user_name_changes");

        migrationBuilder.DropIndex(
            name: "IX_user_name_changes_old_name",
            table: "user_name_changes");

        migrationBuilder.DropIndex(
            name: "IX_user_name_changes_user_id",
            table: "user_name_changes");

        migrationBuilder.DropIndex(
            name: "IX_email_outbox_coalesce_key",
            table: "email_outbox");

        migrationBuilder.DropIndex(
            name: "IX_email_outbox_status_next_attempt_at",
            table: "email_outbox");

        migrationBuilder.AlterColumn<string>(
            name: "email",
            table: "users",
            type: "character varying(320)",
            maxLength: 320,
            nullable: false,
            collation: "ndcoll",
            oldClrType: typeof(string),
            oldType: "character varying(320)",
            oldMaxLength: 320);

        // A plain ALTER COLUMN has no cast from text to inet. Addresses that never were valid become null
        // instead of failing the migration, they could not be shown before either.
        migrationBuilder.Sql(
            """
            ALTER TABLE user_audit_logs
                ALTER COLUMN ip_address TYPE inet
                USING CASE WHEN pg_input_is_valid(ip_address, 'inet') THEN ip_address::inet END;
            """);

        migrationBuilder.AddPrimaryKey(
            name: "user_name_changes_pkey",
            table: "user_name_changes",
            column: "id");

        migrationBuilder.CreateIndex(
            name: "IX_users_email",
            table: "users",
            column: "email",
            unique: true)
            .Annotation("Relational:Collation", new[] { "ndcoll" });

        migrationBuilder.AddCheckConstraint(
            name: "ck_user_shares_max_duration",
            table: "user_shares",
            sql: "max_duration BETWEEN 300 AND 65535");

        migrationBuilder.AddCheckConstraint(
            name: "ck_user_shares_max_intensity",
            table: "user_shares",
            sql: "max_intensity BETWEEN 0 AND 100");

        migrationBuilder.AddCheckConstraint(
            name: "ck_user_share_invite_shockers_max_duration",
            table: "user_share_invite_shockers",
            sql: "max_duration BETWEEN 300 AND 65535");

        migrationBuilder.AddCheckConstraint(
            name: "ck_user_share_invite_shockers_max_intensity",
            table: "user_share_invite_shockers",
            sql: "max_intensity BETWEEN 0 AND 100");

        migrationBuilder.CreateIndex(
            name: "IX_user_name_changes_user_id_created_at",
            table: "user_name_changes",
            columns: new[] { "user_id", "created_at" });

        migrationBuilder.AddCheckConstraint(
            name: "ck_shockers_rf_id",
            table: "shockers",
            sql: "rf_id BETWEEN 0 AND 65535");

        migrationBuilder.AddCheckConstraint(
            name: "ck_shocker_share_codes_max_duration",
            table: "shocker_share_codes",
            sql: "max_duration BETWEEN 300 AND 65535");

        migrationBuilder.AddCheckConstraint(
            name: "ck_shocker_share_codes_max_intensity",
            table: "shocker_share_codes",
            sql: "max_intensity BETWEEN 0 AND 100");

        migrationBuilder.AddCheckConstraint(
            name: "ck_public_share_shockers_cooldown",
            table: "public_share_shockers",
            sql: "cooldown >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "ck_public_share_shockers_max_duration",
            table: "public_share_shockers",
            sql: "max_duration BETWEEN 300 AND 65535");

        migrationBuilder.AddCheckConstraint(
            name: "ck_public_share_shockers_max_intensity",
            table: "public_share_shockers",
            sql: "max_intensity BETWEEN 0 AND 100");

        migrationBuilder.CreateIndex(
            name: "IX_email_outbox_coalesce_key",
            table: "email_outbox",
            column: "coalesce_key",
            filter: "coalesce_key IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_email_outbox_next_attempt_at",
            table: "email_outbox",
            column: "next_attempt_at",
            filter: "status IN ('pending', 'sending')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_api_tokens_shocker_control_duration",
            table: "api_tokens",
            sql: "shocker_control_duration_min >= 300 AND shocker_control_duration_max <= 65535 AND shocker_control_duration_min <= shocker_control_duration_max");

        migrationBuilder.AddCheckConstraint(
            name: "ck_api_tokens_shocker_control_intensity",
            table: "api_tokens",
            sql: "shocker_control_intensity_min >= 0 AND shocker_control_intensity_max <= 100 AND shocker_control_intensity_min <= shocker_control_intensity_max");

        migrationBuilder.Sql(Query_Create_AdminUsersView);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(AddAdminUsersView.Query_Drop_AdminUsersView);

        migrationBuilder.DropIndex(
            name: "IX_users_email",
            table: "users");

        migrationBuilder.DropCheckConstraint(
            name: "ck_user_shares_max_duration",
            table: "user_shares");

        migrationBuilder.DropCheckConstraint(
            name: "ck_user_shares_max_intensity",
            table: "user_shares");

        migrationBuilder.DropCheckConstraint(
            name: "ck_user_share_invite_shockers_max_duration",
            table: "user_share_invite_shockers");

        migrationBuilder.DropCheckConstraint(
            name: "ck_user_share_invite_shockers_max_intensity",
            table: "user_share_invite_shockers");

        migrationBuilder.DropPrimaryKey(
            name: "user_name_changes_pkey",
            table: "user_name_changes");

        migrationBuilder.DropIndex(
            name: "IX_user_name_changes_user_id_created_at",
            table: "user_name_changes");

        migrationBuilder.DropCheckConstraint(
            name: "ck_shockers_rf_id",
            table: "shockers");

        migrationBuilder.DropCheckConstraint(
            name: "ck_shocker_share_codes_max_duration",
            table: "shocker_share_codes");

        migrationBuilder.DropCheckConstraint(
            name: "ck_shocker_share_codes_max_intensity",
            table: "shocker_share_codes");

        migrationBuilder.DropCheckConstraint(
            name: "ck_public_share_shockers_cooldown",
            table: "public_share_shockers");

        migrationBuilder.DropCheckConstraint(
            name: "ck_public_share_shockers_max_duration",
            table: "public_share_shockers");

        migrationBuilder.DropCheckConstraint(
            name: "ck_public_share_shockers_max_intensity",
            table: "public_share_shockers");

        migrationBuilder.DropIndex(
            name: "IX_email_outbox_coalesce_key",
            table: "email_outbox");

        migrationBuilder.DropIndex(
            name: "IX_email_outbox_next_attempt_at",
            table: "email_outbox");

        migrationBuilder.DropCheckConstraint(
            name: "ck_api_tokens_shocker_control_duration",
            table: "api_tokens");

        migrationBuilder.DropCheckConstraint(
            name: "ck_api_tokens_shocker_control_intensity",
            table: "api_tokens");

        migrationBuilder.AlterColumn<string>(
            name: "email",
            table: "users",
            type: "character varying(320)",
            maxLength: 320,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(320)",
            oldMaxLength: 320,
            oldCollation: "ndcoll");

        migrationBuilder.Sql(
            """
            ALTER TABLE user_audit_logs
                ALTER COLUMN ip_address TYPE character varying(40)
                USING host(ip_address);
            """);

        migrationBuilder.AddPrimaryKey(
            name: "user_name_changes_pkey",
            table: "user_name_changes",
            columns: new[] { "id", "user_id" });

        migrationBuilder.CreateIndex(
            name: "IX_users_email",
            table: "users",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_shares_shared_with_user_id",
            table: "user_shares",
            column: "shared_with_user_id");

        migrationBuilder.CreateIndex(
            name: "IX_user_name_changes_created_at",
            table: "user_name_changes",
            column: "created_at");

        migrationBuilder.CreateIndex(
            name: "IX_user_name_changes_old_name",
            table: "user_name_changes",
            column: "old_name");

        migrationBuilder.CreateIndex(
            name: "IX_user_name_changes_user_id",
            table: "user_name_changes",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_email_outbox_coalesce_key",
            table: "email_outbox",
            column: "coalesce_key");

        migrationBuilder.CreateIndex(
            name: "IX_email_outbox_status_next_attempt_at",
            table: "email_outbox",
            columns: new[] { "status", "next_attempt_at" });

        migrationBuilder.Sql(AddAutomationTokens.Query_Create_AdminUsersView);
    }
}
