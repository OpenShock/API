using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

[PgEnum(Name = "ota_update_status")]
public enum OtaUpdateStatus
{
    [PgName("started")] Started = 0,
    [PgName("running")] Running = 1,
    [PgName("finished")] Finished = 2,
    [PgName("error")] Error = 3,
    [PgName("timeout")] Timeout = 4
}
