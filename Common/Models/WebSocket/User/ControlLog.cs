using System.ComponentModel.DataAnnotations;

namespace ShockLink.Common.Models.WebSocket.User;

// ReSharper disable once ClassNeverInstantiated.Global
public class ControlLog
{
    public required GenericIn Shocker { get; set; }
    public required ControlType Type { get; set; }
    [Range(1, 100)] public required byte Intensity { get; set; }
    [Range(300, 30000)] public required uint Duration { get; set; }
    public required DateTimeOffset ExecutedAt { get; set; }
}

public class GenericIn
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}