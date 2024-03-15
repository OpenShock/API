using Microsoft.EntityFrameworkCore.Migrations;
using OpenShock.Common.Models;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AccountService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_name",
                table: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use")
                .Annotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer")
                .OldAnnotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use")
                .OldAnnotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "character varying",
                nullable: false,
                collation: "ndcoll",
                oldClrType: typeof(string),
                oldType: "character varying");

            migrationBuilder.AddColumn<string>(
                name: "password_encryption",
                table: "users",
                type: "password_encryption_type",
                nullable: false,
                defaultValue: "pbkdf2");

            migrationBuilder.CreateIndex(
                name: "idx_name",
                table: "users",
                column: "name")
                .Annotation("Relational:Collation", new[] { "ndcoll" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_encryption",
                table: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use")
                .Annotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer")
                .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use")
                .OldAnnotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "character varying",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldCollation: "ndcoll");

            migrationBuilder.CreateIndex(
                name: "idx_name",
                table: "users",
                column: "name");
        }
    }
}
