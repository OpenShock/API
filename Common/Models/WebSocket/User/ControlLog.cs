using System.ComponentModel.DataAnnotations;
using OpenShock.Common.OpenShockDb;
using OpenShock.Internal.Common.Constants;

namespace OpenShock.Common.Models.WebSocket.User;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ControlLog
{
    public required BasicShockerInfo Shocker { get; init; }

    public required ControlType Type { get; init; }

    [Range(HardLimits.MinControlIntensity, HardLimits.MaxControlIntensity)]
    public required byte Intensity { get; init; }

    [Range(HardLimits.MinControlDuration, HardLimits.MaxControlDuration)]
    public required uint Duration { get; init; }

    public required DateTime ExecutedAt { get; init; }
}
