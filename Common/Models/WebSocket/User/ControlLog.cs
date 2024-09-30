using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.Models.WebSocket.User;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ControlLog
{
    public required GenericIn Shocker { get; set; }

    public required ControlType Type { get; set; }

    [Range(Constants.MinControlIntensity, Constants.MaxControlIntensity)]
    public required byte Intensity { get; set; }

    [Range(Constants.MinControlDuration, Constants.MaxControlDuration)]
    public required uint Duration { get; set; }

    public required DateTime ExecutedAt { get; set; }
}

public class GenericIn
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}