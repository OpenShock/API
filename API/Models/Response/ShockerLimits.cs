using OpenShock.Common;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Response;

public sealed class ShockerLimits
{
    [Range(Constants.MinControlIntensity, Constants.MaxControlIntensity)]
    public required byte? Intensity { get; set; }

    [Range(Constants.MinControlDuration, Constants.MaxControlDuration)]
    public required ushort? Duration { get; set; }
}