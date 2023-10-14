using Semver;

namespace OpenShock.API.Models.WebSocket;

public class DeviceOnlineState
{
    public required Guid Device { get; set; }
    public required bool Online { get; set; }
    public required SemVersion? FirmwareVersion { get; set; }
}