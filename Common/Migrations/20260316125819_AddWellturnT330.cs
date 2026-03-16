using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddWellturnT330 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
                .Annotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .Annotation("Npgsql:Enum:match_type_enum", "exact,contains")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .Annotation("Npgsql:Enum:role_type", "support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR,wellturnT330");

            migrationBuilder.CreateTable(
                name: "configuration",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    description = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("configuration_pkey", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "discord_webhooks",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_id = table.Column<long>(type: "bigint", nullable: false),
                    webhook_token = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("discord_webhooks_pkey", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "email_provider_blacklist",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "ndcoll"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_provider_blacklist_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_name_blacklist",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, collation: "ndcoll"),
                    match_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_name_blacklist_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, collation: "ndcoll"),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, collation: "C"),
                    roles = table.Column<int[]>(type: "role_type[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_token_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_count = table.Column<int>(type: "integer", nullable: false),
                    affected_count = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    ip_country = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_token_reports_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_token_reports_reported_by_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, collation: "C"),
                    created_by_ip = table.Column<IPAddress>(type: "inet", nullable: false),
                    permissions = table.Column<int[]>(type: "permission_type[]", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_used = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "'-infinity'::timestamp without time zone")
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_tokens_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_tokens_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, collation: "C"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("devices_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_devices_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "public_shares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("public_shares_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_public_shares_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_activation_requests",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, collation: "C"),
                    email_send_attempts = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_activation_requests_pkey", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_activation_requests_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_deactivations",
                columns: table => new
                {
                    deactivated_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deactivated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delete_later = table.Column<bool>(type: "boolean", nullable: false),
                    user_moderation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_deactivations_pkey", x => x.deactivated_user_id);
                    table.ForeignKey(
                        name: "fk_user_deactivations_deactivated_by_user_id",
                        column: x => x.deactivated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_deactivations_deactivated_user_id",
                        column: x => x.deactivated_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_email_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_old = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    email_new = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, collation: "C"),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_email_changes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_email_changes_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_name_changes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_name_changes_pkey", x => new { x.id, x.user_id });
                    table.ForeignKey(
                        name: "fk_user_name_changes_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_oauth_connections",
                columns: table => new
                {
                    provider_key = table.Column<string>(type: "text", nullable: false, collation: "C"),
                    external_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_oauth_connections_pkey", x => new { x.provider_key, x.external_id });
                    table.ForeignKey(
                        name: "fk_user_oauth_connections_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_password_resets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "C"),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_password_resets_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_password_resets_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_share_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_share_invites_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_share_invites_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_share_invites_recipient_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_ota_updates",
                columns: table => new
                {
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    update_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "ota_update_status", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    message = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("device_ota_updates_pkey", x => new { x.device_id, x.update_id });
                    table.ForeignKey(
                        name: "fk_device_ota_updates_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shockers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    model = table.Column<int>(type: "shocker_model_type", nullable: false),
                    rf_id = table.Column<int>(type: "integer", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("shockers_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_shockers_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "public_share_shockers",
                columns: table => new
                {
                    public_share_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cooldown = table.Column<int>(type: "integer", nullable: true),
                    allow_shock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_vibrate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_sound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_livecontrol = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    max_intensity = table.Column<byte>(type: "smallint", nullable: true),
                    max_duration = table.Column<int>(type: "integer", nullable: true),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("public_share_shockers_pkey", x => new { x.public_share_id, x.shocker_id });
                    table.ForeignKey(
                        name: "fk_public_share_shockers_public_share_id",
                        column: x => x.public_share_id,
                        principalTable: "public_shares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_public_share_shockers_shocker_id",
                        column: x => x.shocker_id,
                        principalTable: "shockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shocker_control_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    controlled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    intensity = table.Column<byte>(type: "smallint", nullable: false),
                    duration = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "control_type", nullable: false),
                    custom_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    live_control = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("shocker_control_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_shocker_control_logs_controlled_by_user_id",
                        column: x => x.controlled_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shocker_control_logs_shocker_id",
                        column: x => x.shocker_id,
                        principalTable: "shockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shocker_share_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    allow_shock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_vibrate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_sound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_livecontrol = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    max_intensity = table.Column<byte>(type: "smallint", nullable: true),
                    max_duration = table.Column<int>(type: "integer", nullable: true),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shocker_share_codes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_shocker_share_codes_shocker_id",
                        column: x => x.shocker_id,
                        principalTable: "shockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_share_invite_shockers",
                columns: table => new
                {
                    invite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allow_shock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_vibrate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_sound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_livecontrol = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    max_intensity = table.Column<byte>(type: "smallint", nullable: true),
                    max_duration = table.Column<int>(type: "integer", nullable: true),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_share_invite_shockers_pkey", x => new { x.invite_id, x.shocker_id });
                    table.ForeignKey(
                        name: "fk_user_share_invite_shockers_invite_id",
                        column: x => x.invite_id,
                        principalTable: "user_share_invites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_share_invite_shockers_shocker_id",
                        column: x => x.shocker_id,
                        principalTable: "shockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_shares",
                columns: table => new
                {
                    shared_with_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    allow_shock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_vibrate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_sound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_livecontrol = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    max_intensity = table.Column<byte>(type: "smallint", nullable: true),
                    max_duration = table.Column<int>(type: "integer", nullable: true),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_shares_pkey", x => new { x.shared_with_user_id, x.shocker_id });
                    table.ForeignKey(
                        name: "fk_user_shares_shared_with_user_id",
                        column: x => x.shared_with_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_shares_shocker_id",
                        column: x => x.shocker_id,
                        principalTable: "shockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_token_reports_user_id",
                table: "api_token_reports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_token_hash",
                table: "api_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_user_id",
                table: "api_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_valid_until",
                table: "api_tokens",
                column: "valid_until");

            migrationBuilder.CreateIndex(
                name: "device_ota_updates_created_at_idx",
                table: "device_ota_updates",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_devices_owner_id",
                table: "devices",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_devices_token",
                table: "devices",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_provider_blacklist_domain",
                table: "email_provider_blacklist",
                column: "domain",
                unique: true)
                .Annotation("Relational:Collation", new[] { "ndcoll" });

            migrationBuilder.CreateIndex(
                name: "IX_public_share_shockers_shocker_id",
                table: "public_share_shockers",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_public_shares_owner_id",
                table: "public_shares",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_control_logs_controlled_by_user_id",
                table: "shocker_control_logs",
                column: "controlled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_control_logs_shocker_id",
                table: "shocker_control_logs",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_share_codes_shocker_id",
                table: "shocker_share_codes",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_shockers_device_id",
                table: "shockers",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_activation_requests_token_hash",
                table: "user_activation_requests",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_deactivations_deactivated_by_user_id",
                table: "user_deactivations",
                column: "deactivated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_email_changes_created_at",
                table: "user_email_changes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_email_changes_used_at",
                table: "user_email_changes",
                column: "used_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_email_changes_user_id",
                table: "user_email_changes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_name_blacklist_value",
                table: "user_name_blacklist",
                column: "value",
                unique: true)
                .Annotation("Relational:Collation", new[] { "ndcoll" });

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
                name: "IX_user_oauth_connections_user_id",
                table: "user_oauth_connections",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_password_resets_user_id",
                table: "user_password_resets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_share_invite_shockers_shocker_id",
                table: "user_share_invite_shockers",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_share_invites_owner_id",
                table: "user_share_invites",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_share_invites_user_id",
                table: "user_share_invites",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_shares_shared_with_user_id",
                table: "user_shares",
                column: "shared_with_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_shares_shocker_id",
                table: "user_shares",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_name",
                table: "users",
                column: "name",
                unique: true)
                .Annotation("Relational:Collation", new[] { "ndcoll" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_token_reports");

            migrationBuilder.DropTable(
                name: "api_tokens");

            migrationBuilder.DropTable(
                name: "configuration");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "device_ota_updates");

            migrationBuilder.DropTable(
                name: "discord_webhooks");

            migrationBuilder.DropTable(
                name: "email_provider_blacklist");

            migrationBuilder.DropTable(
                name: "public_share_shockers");

            migrationBuilder.DropTable(
                name: "shocker_control_logs");

            migrationBuilder.DropTable(
                name: "shocker_share_codes");

            migrationBuilder.DropTable(
                name: "user_activation_requests");

            migrationBuilder.DropTable(
                name: "user_deactivations");

            migrationBuilder.DropTable(
                name: "user_email_changes");

            migrationBuilder.DropTable(
                name: "user_name_blacklist");

            migrationBuilder.DropTable(
                name: "user_name_changes");

            migrationBuilder.DropTable(
                name: "user_oauth_connections");

            migrationBuilder.DropTable(
                name: "user_password_resets");

            migrationBuilder.DropTable(
                name: "user_share_invite_shockers");

            migrationBuilder.DropTable(
                name: "user_shares");

            migrationBuilder.DropTable(
                name: "public_shares");

            migrationBuilder.DropTable(
                name: "user_share_invites");

            migrationBuilder.DropTable(
                name: "shockers");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
