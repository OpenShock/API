using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthSupport : Migration
    {
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
            
            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldCollation: "C");
            
            migrationBuilder.Sql(Query_Create_AdminUsersView);

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

            migrationBuilder.CreateIndex(
                name: "IX_user_oauth_connections_user_id",
                table: "user_oauth_connections",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "user_oauth_connections");

            migrationBuilder.Sql(AddAdminUsersView.Query_Drop_AdminUsersView);
            
            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                collation: "C",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldCollation: "C");
            
            migrationBuilder.Sql(CleanupTokenHashColumns.Query_Create_AdminUsersView);
        }
    }
}
