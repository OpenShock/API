using System.Text.Json.Serialization;
using NpgsqlTypes;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Utils;

// ReSharper disable InconsistentNaming

namespace OpenShock.Common.OpenShockDb;

[PgEnum(Name = "permission_type")]
[JsonConverter(typeof(PermissionTypeConverter))]
public enum PermissionType
{
    [PgName("shockers.use")] Shockers_Use = 0,

    [PgName("shockers.edit")] Shockers_Edit = 1,

    [PgName("shockers.pause")] Shockers_Pause = 2,

    [PgName("devices.edit")] Devices_Edit = 3,

    [PgName("devices.auth")] Devices_Auth = 4,
        
    [PgName("usershares.edit")] Usershares_Edit = 5,
    
    [PgName("usershares.pause")] Usershares_Pause = 6,
    [PgName("publicshares.pause")] Publicshares_Pause = 7,
    [PgName("publicshares.edit")] Publicshares_Edit = 8,
}
