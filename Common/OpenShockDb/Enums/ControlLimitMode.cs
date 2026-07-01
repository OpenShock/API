using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Determines how a per-token min/max limit is applied to an incoming control value.
/// </summary>
[PgEnum(Name = "control_limit_mode")]
public enum ControlLimitMode
{
    /// <summary>
    /// Clamp the incoming value into the [min, max] range.
    /// </summary>
    [PgName("clamp")] Clamp = 0,

    /// <summary>
    /// Linearly remap the full input range onto [min, max].
    /// </summary>
    [PgName("lerp")] Lerp = 1
}
