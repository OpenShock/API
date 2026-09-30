using Microsoft.EntityFrameworkCore.Migrations;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddApiTokenShockerControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .OldAnnotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .OldAnnotation("Npgsql:Enum:match_type_enum", "exact,contains")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .OldAnnotation("Npgsql:Enum:role_type", "support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR,wellturnT330");

            migrationBuilder.AddColumn<int>(
                name: "shocker_control_duration_max",
                table: "api_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 65535);

            migrationBuilder.AddColumn<int>(
                name: "shocker_control_duration_min",
                table: "api_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 300);

            migrationBuilder.AddColumn<ControlLimitMode>(
                name: "shocker_control_duration_mode",
                table: "api_tokens",
                type: "control_limit_mode",
                nullable: false,
                defaultValue: ControlLimitMode.Clamp);

            migrationBuilder.AddColumn<byte>(
                name: "shocker_control_intensity_max",
                table: "api_tokens",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)100);

            migrationBuilder.AddColumn<byte>(
                name: "shocker_control_intensity_min",
                table: "api_tokens",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<ControlLimitMode>(
                name: "shocker_control_intensity_mode",
                table: "api_tokens",
                type: "control_limit_mode",
                nullable: false,
                defaultValue: ControlLimitMode.Clamp);

            migrationBuilder.AddColumn<bool>(
                name: "shocker_control_paused",
                table: "api_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shocker_control_duration_max",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "shocker_control_duration_min",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "shocker_control_duration_mode",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "shocker_control_intensity_max",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "shocker_control_intensity_min",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "shocker_control_intensity_mode",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "shocker_control_paused",
                table: "api_tokens");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
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
                .OldAnnotation("Npgsql:Enum:match_type_enum", "exact,contains")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .OldAnnotation("Npgsql:Enum:role_type", "support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR,wellturnT330");
        }
    }
}
