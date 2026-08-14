using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenShock.Common.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPgEnums : Migration
    {
        // Postgres cannot reorder or remove labels of an existing enum type in place, and
        // ALTER TYPE ... ADD VALUE is the only thing EF's annotation diff emits. So any change
        // beyond appending a label silently no-ops (reorder) or leaves stale labels behind
        // (rename). To apply these changes robustly we recreate each affected type:
        //   detach columns to text -> drop the old type -> (remap values) -> create the new type -> reattach.
        // This works regardless of the current label set/order and is fully reversible.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // control_type: reorder labels to match the C# enum's declaration order.
            migrationBuilder.Sql("ALTER TABLE shocker_control_logs ALTER COLUMN type TYPE text USING type::text;");
            migrationBuilder.Sql("DROP TYPE control_type;");
            migrationBuilder.Sql("CREATE TYPE control_type AS ENUM ('stop', 'shock', 'vibrate', 'sound');");
            migrationBuilder.Sql("ALTER TABLE shocker_control_logs ALTER COLUMN type TYPE control_type USING type::control_type;");

            // password_encryption_type: reorder labels. Orphan type (no column maps to it).
            migrationBuilder.Sql("DROP TYPE password_encryption_type;");
            migrationBuilder.Sql("CREATE TYPE password_encryption_type AS ENUM ('bcrypt_enhanced', 'pbkdf2');");

            // shocker_model_type: rename labels to snake_case (and fix the petTrainer typo).
            migrationBuilder.Sql("ALTER TABLE shockers ALTER COLUMN model TYPE text USING model::text;");
            migrationBuilder.Sql("DROP TYPE shocker_model_type;");
            migrationBuilder.Sql(
                """
                UPDATE shockers SET model = CASE model
                    WHEN 'caiXianlin' THEN 'cai_xianlin'
                    WHEN 'petTrainer' THEN 'petrainer'
                    WHEN 'petrainer998DR' THEN 'petrainer_998dr'
                    WHEN 'wellturnT330' THEN 'wellturn_t330'
                    ELSE model
                END;
                """);
            migrationBuilder.Sql("CREATE TYPE shocker_model_type AS ENUM ('cai_xianlin', 'petrainer', 'petrainer_998dr', 'wellturn_t330');");
            migrationBuilder.Sql("ALTER TABLE shockers ALTER COLUMN model TYPE shocker_model_type USING model::shocker_model_type;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // control_type: restore the original label order.
            migrationBuilder.Sql("ALTER TABLE shocker_control_logs ALTER COLUMN type TYPE text USING type::text;");
            migrationBuilder.Sql("DROP TYPE control_type;");
            migrationBuilder.Sql("CREATE TYPE control_type AS ENUM ('sound', 'vibrate', 'shock', 'stop');");
            migrationBuilder.Sql("ALTER TABLE shocker_control_logs ALTER COLUMN type TYPE control_type USING type::control_type;");

            // password_encryption_type: restore the original label order.
            migrationBuilder.Sql("DROP TYPE password_encryption_type;");
            migrationBuilder.Sql("CREATE TYPE password_encryption_type AS ENUM ('pbkdf2', 'bcrypt_enhanced');");

            // shocker_model_type: restore the original camelCase labels.
            migrationBuilder.Sql("ALTER TABLE shockers ALTER COLUMN model TYPE text USING model::text;");
            migrationBuilder.Sql("DROP TYPE shocker_model_type;");
            migrationBuilder.Sql(
                """
                UPDATE shockers SET model = CASE model
                    WHEN 'cai_xianlin' THEN 'caiXianlin'
                    WHEN 'petrainer' THEN 'petTrainer'
                    WHEN 'petrainer_998dr' THEN 'petrainer998DR'
                    WHEN 'wellturn_t330' THEN 'wellturnT330'
                    ELSE model
                END;
                """);
            migrationBuilder.Sql("CREATE TYPE shocker_model_type AS ENUM ('caiXianlin', 'petTrainer', 'petrainer998DR', 'wellturnT330');");
            migrationBuilder.Sql("ALTER TABLE shockers ALTER COLUMN model TYPE shocker_model_type USING model::shocker_model_type;");
        }
    }
}
