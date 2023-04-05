using NpgsqlTypes;

namespace ShockLink.Common.Models;

public enum PermissionType
{
    [PgName("shockers.use")]
    ShockersUse
}