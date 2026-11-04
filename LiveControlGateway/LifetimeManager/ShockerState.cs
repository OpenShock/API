using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;

using OpenShock.Internal.Common.Constants;

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
    /// User owning the shocker
    /// </summary>
    public required Guid OwnerId { get; init; }

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
    /// Last live control frame received for the shocker. Replaced as a whole on every frame, so a reader always
    /// sees one frame's type, intensity, controller and expiry together, never a mix of two frames.
    /// </summary>
    public LiveFrame? LastFrame { get; set; }

    /// <summary>
    /// Whether the shocker was emitting live control on the previous tick, with which type and by whom.
    /// Used to count continuous bursts for stats.
    /// </summary>
    public (ControlType Type, Guid ControllerId)? EmittingOnLastTick { get; set; }
    
    /// <summary>
    /// When an exclusive command is sent to the shocker, this is the time it will be exclusive until so we dont allow other live commands
    /// </summary>
    public DateTimeOffset ExclusiveUntil = DateTimeOffset.MinValue;
}

/// <summary>
/// A live control frame as received from a client
/// </summary>
/// <param name="Type">Control type</param>
/// <param name="Intensity">Intensity, already clamped to the controller's permissions</param>
/// <param name="ControllerId">User who sent the frame</param>
/// <param name="ActiveUntil">Until when the frame keeps the shocker going, determined by client TPS interval + receive time</param>
public sealed record LiveFrame(
    ControlType Type,
    [property: Range(HardLimits.MinControlIntensity, HardLimits.MaxControlIntensity)] byte Intensity,
    Guid ControllerId,
    DateTimeOffset ActiveUntil);
