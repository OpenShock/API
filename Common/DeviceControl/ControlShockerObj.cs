using OpenShock.Common.Models;

namespace OpenShock.Common.DeviceControl;

public sealed class ControlShockerObj
{
    public required Guid ShockerId { get; init; }
    public required string ShockerName { get; init; }
    public required ushort ShockerRfId { get; init; }
    public required Guid DeviceId { get; init; }
    public required ShockerModelType ShockerModel { get; init; }
    public required Guid OwnerId { get; init; }
    public required bool Paused { get; init; }
    public required SharePermsAndLimits? PermsAndLimits { get; init; }
}