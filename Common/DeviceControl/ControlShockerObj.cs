using OpenShock.Common.Models;

namespace OpenShock.Common.DeviceControl;

public sealed class ControlShockerObj
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required ushort RfId { get; set; }
    public required Guid Device { get; set; }
    public required Guid Owner { get; set; }
    public required ShockerModelType Model { get; set; }
    public required bool Paused { get; set; }
    public required SharePermsAndLimits? PermsAndLimits { get; set; }
}