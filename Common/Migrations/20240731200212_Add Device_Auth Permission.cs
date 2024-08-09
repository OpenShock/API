using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddDevice_AuthPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .Annotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR")
                .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit")
                .OldAnnotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit")
                .Annotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR")
                .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .OldAnnotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer,petrainer998DR");
        }
    }
}
