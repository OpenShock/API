using System.Net;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;

using OpenShock.Internal.Common.Constants;

namespace OpenShock.Common.OpenShockDb;

public sealed class ApiToken
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required string Name { get; set; }

    public required string TokenHash { get; set; }

    public required IPAddress CreatedByIp { get; set; }

    public required List<PermissionType> Permissions { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// When true, this token is paused and may not send shocker control messages.
    /// Independent gate on top of the <see cref="PermissionType.Shockers_Use"/> permission.
    /// </summary>
    public bool ShockerControlPaused { get; set; } = false;

    public byte ShockerControlIntensityMin { get; set; } = HardLimits.MinControlIntensity;

    public byte ShockerControlIntensityMax { get; set; } = HardLimits.MaxControlIntensity;

    public ControlLimitMode ShockerControlIntensityMode { get; set; } = ControlLimitMode.Clamp;

    public ushort ShockerControlDurationMin { get; set; } = HardLimits.MinControlDuration;

    public ushort ShockerControlDurationMax { get; set; } = HardLimits.MaxControlDuration;

    public ControlLimitMode ShockerControlDurationMode { get; set; } = ControlLimitMode.Clamp;

    // Navigations
    public User User { get; set; } = null!;
}
