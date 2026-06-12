using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Models;

[PgEnum]
public enum OtaUpdateStatus
{
    [PgName("started")] Started,
    [PgName("running")] Running,
    [PgName("finished")] Finished,
    [PgName("error")] Error,
    [PgName("timeout")] Timeout,
}
