using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.API.Models.Response;

public sealed class ShockerLimits
{
    [Range(HardLimits.MinControlIntensity, HardLimits.MaxControlIntensity)]
    public required byte? Intensity { get; set; }

    [Range(HardLimits.MinControlDuration, HardLimits.MaxControlDuration)]
    public required ushort? Duration { get; set; }
}