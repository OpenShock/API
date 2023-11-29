using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Models;

namespace OpenShock.LiveControlGateway.LifetimeManager;

public class ShockerState
{
    // Backend Configuration Variables
    
    public required Guid Id { get; init; }
    public required ushort RfId { get; init; }
    public required ShockerModelType Model { get; init; }
    
    // Last state

    [Range(0, 100)]
    public byte LastIntensity { get; set; } = 0;
    public ControlType LastType { get; set; } = ControlType.Stop;
    public DateTimeOffset LastReceive = DateTimeOffset.MinValue;
}