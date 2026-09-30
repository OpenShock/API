using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

[PgEnum(Name = "configuration_value_type")]
public enum ConfigurationValueType
{
    [PgName("string")] String = 0,
    [PgName("bool")] Bool = 1,
    [PgName("int")] Int = 2,
    [PgName("float")] Float = 3,
    [PgName("json")] Json = 4
}
