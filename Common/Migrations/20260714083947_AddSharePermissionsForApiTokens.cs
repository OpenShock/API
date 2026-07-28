using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddSharePermissionsForApiTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted")
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
                .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted")
                .OldAnnotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
                .OldAnnotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
                .OldAnnotation("Npgsql:Enum:control_type", "stop,shock,vibrate,sound")
                .OldAnnotation("Npgsql:Enum:email_status", "pending,sending,sent,failed,skipped")
                .OldAnnotation("Npgsql:Enum:email_type", "account_activation,password_reset,email_verification,email_change_notice")
                .OldAnnotation("Npgsql:Enum:match_type_enum", "exact,contains")
                .OldAnnotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .OldAnnotation("Npgsql:Enum:password_encryption_type", "bcrypt_enhanced,pbkdf2")
                .OldAnnotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .OldAnnotation("Npgsql:Enum:role_type", "support,staff,admin,system")
                .OldAnnotation("Npgsql:Enum:shocker_model_type", "cai_xianlin,petrainer,petrainer_998dr,wellturn_t330");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted")
                .Annotation("Npgsql:Enum:configuration_value_type", "string,bool,int,float,json")
                .Annotation("Npgsql:Enum:control_limit_mode", "clamp,lerp")
                .Annotation("Npgsql:Enum:control_type", "stop,shock,vibrate,sound")
                .Annotation("Npgsql:Enum:email_status", "pending,sending,sent,failed,skipped")
                .Annotation("Npgsql:Enum:email_type", "account_activation,password_reset,email_verification,email_change_notice")
                .Annotation("Npgsql:Enum:match_type_enum", "exact,contains")
                .Annotation("Npgsql:Enum:ota_update_status", "started,running,finished,error,timeout")
                .Annotation("Npgsql:Enum:password_encryption_type", "bcrypt_enhanced,pbkdf2")
                .Annotation("Npgsql:Enum:permission_type", "shockers.use,shockers.edit,shockers.pause,devices.edit,devices.auth")
                .Annotation("Npgsql:Enum:role_type", "support,staff,admin,system")
                .Annotation("Npgsql:Enum:shocker_model_type", "cai_xianlin,petrainer,petrainer_998dr,wellturn_t330")
                .OldAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:audit_action", "login,logout,password_changed,email_change_requested,email_changed,username_changed,api_token_created,api_token_deleted,oauth_connected,oauth_disconnected,account_deactivated,account_reactivated,account_deleted")
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
        }
    }
}
