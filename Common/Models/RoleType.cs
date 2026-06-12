using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Models;

[PgEnum]
public enum RoleType
{
    [PgName("support")] Support,
    [PgName("staff")] Staff,
    [PgName("admin")] Admin,
    [PgName("system")] System,
}
