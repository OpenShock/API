using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.Models.WebSocket.User;

// ReSharper disable once ClassNeverInstantiated.Global
public class Control
{
    public required Guid Id { get; set; }

    [EnumDataType(typeof(ControlType))]
    public required ControlType Type { get; set; }

    [Range(Constants.MinControlIntensity, Constants.MaxControlIntensity)]
    public required byte Intensity { get; set; }

    [Range(Constants.MinControlDuration, Constants.MaxControlDuration)]
    public required ushort Duration { get; set; }

    /// <summary>
    /// If true, overrides livecontrol
    /// </summary>
    public bool Exclusive { get; set; } = false;
}