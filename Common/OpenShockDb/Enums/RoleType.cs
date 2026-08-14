using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

[PgEnum(Name = "role_type")]
public enum RoleType
{
    [PgName("support")] Support = 0,
    [PgName("staff")] Staff = 1,
    [PgName("admin")] Admin = 2,
    [PgName("system")] System = 3,
}
