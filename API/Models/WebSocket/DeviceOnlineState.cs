using System.Text.Json.Serialization;
using OpenShock.Common.Serialization;
using Semver;

namespace OpenShock.API.Models.WebSocket;

public class DeviceOnlineState
{
    public required Guid Device { get; set; }
    public required bool Online { get; set; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion? FirmwareVersion { get; set; }
}