namespace OpenShock.Common.Models;

/// <summary>
/// Determines how a per-token min/max limit is applied to an incoming control value.
/// </summary>
public enum ControlLimitMode
{
    /// <summary>
    /// Clamp the incoming value into the [min, max] range.
    /// </summary>
    Clamp = 0,

    /// <summary>
    /// Linearly remap the full input range onto [min, max].
    /// </summary>
    Lerp = 1
}
