using Microsoft.EntityFrameworkCore.Migrations;
using OpenShock.Common.Models;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class DbCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ### CUSTOM SQL BEGIN ###

            // Update the password hashes prefix BEFORE dropping the column
            migrationBuilder.Sql("UPDATE users SET password = CONCAT('pbkdf2:', substring(password from 5)) WHERE password LIKE 'USER$%'");
            migrationBuilder.Sql("UPDATE users SET password = CONCAT('bcrypt:', password) WHERE password NOT LIKE 'pbkdf2:%'");

            // #### CUSTOM SQL END ####

            migrationBuilder.DropColumn(
                name: "password_encryption",
                table: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
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

            migrationBuilder.RenameColumn(
                name: "password",
                table: "users",
                newName: "password_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "users",
                newName: "password");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:password_encryption_type", "pbkdf2,bcrypt_enhanced")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use")
                .Annotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer")
                .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:control_type", "sound,vibrate,shock,stop")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use")
                .OldAnnotation("Npgsql:Enum:rank_type", "user,support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "caiXianlin,petTrainer");

            migrationBuilder.AddColumn<string>(
                name: "password_encryption",
                table: "users",
                type: "password_encryption_type",
                nullable: false,
                defaultValue: "pbkdf2");

            // ### CUSTOM SQL BEGIN ###

            // Populate the password_encryption column BEFORE updating the password hashes prefix
            migrationBuilder.Sql("UPDATE users SET password_encryption = 'pbkdf2' WHERE password LIKE 'pbkdf2:%'");
            migrationBuilder.Sql("UPDATE users SET password_encryption = 'bcrypt_enhanced' WHERE password LIKE 'bcrypt:%'");

            // Update the password hashes prefix AFTER updating the password_encryption column
            migrationBuilder.Sql("UPDATE users SET password = SUBSTRING(password FROM 8) WHERE password LIKE 'pbkdf2:%'");
            migrationBuilder.Sql("UPDATE users SET password = SUBSTRING(password FROM 8) WHERE password LIKE 'bcrypt:%'");

            // #### CUSTOM SQL END ####
        }
    }
}
