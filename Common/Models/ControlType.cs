using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Models;

[PgEnum]
public enum ControlType
{
    [PgName("stop")] Stop = 0,
    [PgName("shock")] Shock = 1,
    [PgName("vibrate")] Vibrate = 2,
    [PgName("sound")] Sound = 3,
}
