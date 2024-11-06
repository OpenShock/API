using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.Common.Models.WebSocket.User;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Control
{
    public required Guid Id { get; set; }

    [EnumDataType(typeof(ControlType))]
    public required ControlType Type { get; set; }

    [Range(HardLimits.MinControlIntensity, HardLimits.MaxControlIntensity)]
    public required byte Intensity { get; set; }

    [Range(HardLimits.MinControlDuration, HardLimits.MaxControlDuration)]
    public required ushort Duration { get; set; }

    /// <summary>
    /// If true, overrides livecontrol
    /// </summary>
    public bool Exclusive { get; set; } = false;
}