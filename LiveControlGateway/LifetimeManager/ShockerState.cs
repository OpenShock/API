using System.ComponentModel.DataAnnotations;
using OpenShock.Common;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;

namespace OpenShock.LiveControlGateway.LifetimeManager;

/// <summary>
/// Represents the state of a single shocker within a device lifetime
/// </summary>
public sealed class ShockerState
{
    // Backend Configuration Variables
    
    /// <summary>
    /// Shocker Id
    /// </summary>
    public required Guid Id { get; init; }
    
    /// <summary>
    /// RfId of the shocker
    /// </summary>
    public required ushort RfId { get; init; }
    
    /// <summary>
    /// Model type of the shocker
    /// </summary>
    public required ShockerModelType Model { get; init; }
    
    // Last state

    /// <summary>
    /// Last intensity sent to the shocker via live control
    /// </summary>
    [Range(HardLimits.MinControlIntensity, HardLimits.MaxControlIntensity)]
    public byte LastIntensity { get; set; } = 0;
    
    /// <summary>
    /// Last duration sent to the shocker via live control
    /// </summary>
    public ControlType LastType { get; set; } = ControlType.Stop;
    
    /// <summary>
    /// Active until time for the shocker, determined by client TPS interval + current time
    /// </summary>
    public DateTimeOffset ActiveUntil = DateTimeOffset.MinValue;
    
    /// <summary>
    /// When an exclusive command is sent to the shocker, this is the time it will be exclusive until so we dont allow other live commands
    /// </summary>
    public DateTimeOffset ExclusiveUntil = DateTimeOffset.MinValue;
}