using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
                .Annotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
                .Annotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .Annotation("Npgsql:Enum:email_status", "pending,sending,sent,failed,skipped")
                .Annotation("Npgsql:Enum:email_type", "account_activation,password_reset,email_verification,email_change_notice")
                .Annotation("Npgsql:Enum:match_type_enum", "exact,contains")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .Annotation("Npgsql:Enum:role_type", "support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR,wellturnT330")
                .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
                .OldAnnotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
                .OldAnnotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .OldAnnotation("Npgsql:Enum:match_type_enum", "exact,contains")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .OldAnnotation("Npgsql:Enum:role_type", "support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR,wellturnT330");

            migrationBuilder.CreateTable(
                name: "email_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<EmailType>(type: "email_type", nullable: false),
                    recipient = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    payload = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    coalesce_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<EmailStatus>(type: "email_status", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_error = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_outbox_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_coalesce_key",
                table: "email_outbox",
                column: "coalesce_key");

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_recipient",
                table: "email_outbox",
                column: "recipient");

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_status_next_attempt_at",
                table: "email_outbox",
                columns: new[] { "status", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_outbox");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
                .Annotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
                .Annotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .Annotation("Npgsql:Enum:match_type_enum", "exact,contains")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .Annotation("Npgsql:Enum:role_type", "support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR,wellturnT330")
                .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
                .OldAnnotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
                .OldAnnotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .OldAnnotation("Npgsql:Enum:email_status", "pending,sending,sent,failed,skipped")
                .OldAnnotation("Npgsql:Enum:email_type", "account_activation,password_reset,email_verification,email_change_notice")
                .OldAnnotation("Npgsql:Enum:match_type_enum", "exact,contains")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .OldAnnotation("Npgsql:Enum:role_type", "support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR,wellturnT330");
        }
    }
}
