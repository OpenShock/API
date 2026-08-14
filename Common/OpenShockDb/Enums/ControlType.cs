using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

[PgEnum(Name = "control_type")]
public enum ControlType
{
    [PgName("stop")] Stop = 0,
    [PgName("shock")] Shock = 1,
    [PgName("vibrate")] Vibrate = 2,
    [PgName("sound")] Sound = 3
}
