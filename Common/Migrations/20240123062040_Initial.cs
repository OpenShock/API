using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use")
                .Annotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying", nullable: false),
                    email = table.Column<string>(type: "character varying", nullable: false),
                    password = table.Column<string>(type: "character varying", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    email_actived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    rank = table.Column<int>(type: "rank_type", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by_ip = table.Column<string>(type: "character varying", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    permissions = table.Column<int[]>(type: "permission_type[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_tokens_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_id",
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
                    owner = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("devices_pkey", x => x.id);
                    table.ForeignKey(
                        name: "owner_user_id",
                        column: x => x.owner,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_resets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    used_on = table.Column<DateTimeOffset>(type: "time with time zone", nullable: true),
                    secret = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("password_resets_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shocker_shares_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shocker_shares_links_pkey", x => x.id);
                    table.ForeignKey(
                        name: "owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_ota_updates",
                columns: table => new
                {
                    device = table.Column<Guid>(type: "uuid", nullable: false),
                    update_id = table.Column<int>(type: "integer", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    version = table.Column<string>(type: "character varying", nullable: false),
                    status = table.Column<int>(type: "ota_update_status", nullable: false),
                    message = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("device_ota_updates_pkey", x => new { x.device, x.update_id });
                    table.ForeignKey(
                        name: "device_ota_updates_device",
                        column: x => x.device,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shockers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rf_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    device = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    model = table.Column<int>(type: "shocker_model_type", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shockers_pkey", x => x.id);
                    table.ForeignKey(
                        name: "device_id",
                        column: x => x.device,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shocker_control_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    controlled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    intensity = table.Column<byte>(type: "smallint", nullable: false),
                    duration = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "control_type", nullable: false),
                    custom_name = table.Column<string>(type: "character varying", nullable: true),
                    live_control = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shocker_control_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_controlled_by",
                        column: x => x.controlled_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shocker_id",
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
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    perm_sound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    perm_vibrate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    perm_shock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    limit_duration = table.Column<int>(type: "integer", nullable: true),
                    limit_intensity = table.Column<byte>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shocker_share_codes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_shocker_id",
                        column: x => x.shocker_id,
                        principalTable: "shockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shocker_shares",
                columns: table => new
                {
                    shocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shared_with = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    perm_sound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    perm_vibrate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    perm_shock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    limit_duration = table.Column<int>(type: "integer", nullable: true),
                    limit_intensity = table.Column<byte>(type: "smallint", nullable: true),
                    paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    perm_live = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shocker_shares_pkey", x => new { x.shocker_id, x.shared_with });
                    table.ForeignKey(
                        name: "ref_shocker_id",
                        column: x => x.shocker_id,
                        principalTable: "shockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "shared_with_user_id",
                        column: x => x.shared_with,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shocker_shares_links_shockers",
                columns: table => new
                {
                    share_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    perm_sound = table.Column<bool>(type: "boolean", nullable: false),
                    perm_vibrate = table.Column<bool>(type: "boolean", nullable: false),
                    perm_shock = table.Column<bool>(type: "boolean", nullable: false),
                    limit_duration = table.Column<int>(type: "integer", nullable: true),
                    limit_intensity = table.Column<byte>(type: "smallint", nullable: true),
                    cooldown = table.Column<int>(type: "integer", nullable: true),
                    paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    perm_live = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("shocker_shares_links_shockers_pkey", x => new { x.share_link_id, x.shocker_id });
                    table.ForeignKey(
                        name: "share_link_id",
                        column: x => x.share_link_id,
                        principalTable: "shocker_shares_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "shocker_id",
                        column: x => x.shocker_id,
                        principalTable: "shockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_user_id",
                table: "api_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "device_ota_updates_created_on_idx",
                table: "device_ota_updates",
                column: "created_on")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_devices_owner",
                table: "devices",
                column: "owner");

            migrationBuilder.CreateIndex(
                name: "IX_password_resets_user_id",
                table: "password_resets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_control_logs_controlled_by",
                table: "shocker_control_logs",
                column: "controlled_by");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_control_logs_shocker_id",
                table: "shocker_control_logs",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_share_codes_shocker_id",
                table: "shocker_share_codes",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_shares_shared_with",
                table: "shocker_shares",
                column: "shared_with");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_shares_links_owner_id",
                table: "shocker_shares_links",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_shocker_shares_links_shockers_shocker_id",
                table: "shocker_shares_links_shockers",
                column: "shocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_shockers_device",
                table: "shockers",
                column: "device");

            migrationBuilder.CreateIndex(
                name: "email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_email",
                table: "users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_name",
                table: "users",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "username",
                table: "users",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_tokens");

            migrationBuilder.DropTable(
                name: "device_ota_updates");

            migrationBuilder.DropTable(
                name: "password_resets");

            migrationBuilder.DropTable(
                name: "shocker_control_logs");

            migrationBuilder.DropTable(
                name: "shocker_share_codes");

            migrationBuilder.DropTable(
                name: "shocker_shares");

            migrationBuilder.DropTable(
                name: "shocker_shares_links_shockers");

            migrationBuilder.DropTable(
                name: "shocker_shares_links");

            migrationBuilder.DropTable(
                name: "shockers");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
