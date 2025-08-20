using OpenShock.Common.Models;

namespace OpenShock.Common.DeviceControl;

public sealed class ControlShockerObj
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required ushort RfId { get; init; }
    public required Guid Device { get; init; }
    public required ShockerModelType Model { get; init; }
    public required Guid OwnerId { get; init; }
    public required bool Paused { get; init; }
    public required SharePermsAndLimits? PermsAndLimits { get; init; }
}