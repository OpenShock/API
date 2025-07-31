using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.Common.Models.WebSocket.User;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ControlLog
{
    public required BasicShockerInfo Shocker { get; set; }

    public required ControlType Type { get; set; }

    [Range(HardLimits.MinControlIntensity, HardLimits.MaxControlIntensity)]
    public required byte Intensity { get; set; }

    [Range(HardLimits.MinControlDuration, HardLimits.MaxControlDuration)]
    public required uint Duration { get; set; }

    public required DateTime ExecutedAt { get; set; }
}