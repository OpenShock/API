using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Metrics;

/// <summary>
/// Tag values for <see cref="ControlType"/>, shared by the API and the gateway so a shock counts as
/// the same thing on both sides.
/// </summary>
public static class ControlTypeTag
{
    /// <summary>
    /// The label value for a control type. Matches the database's <c>control_type</c> spelling.
    /// </summary>
    /// <remarks>
    /// Not <c>ToString()</c>: that allocates on every measurement, and it would pin the scraped label
    /// to the C# casing, so renaming the enum member would silently break every query and dashboard.
    /// </remarks>
    /// <param name="type"></param>
    public static string Of(ControlType type) => type switch
    {
        ControlType.Stop => "stop",
        ControlType.Shock => "shock",
        ControlType.Vibrate => "vibrate",
        ControlType.Sound => "sound",
        _ => "unknown"
    };
}
