using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;

namespace OpenShock.Common.Models.WebSocket;

public sealed class DeviceOnlineState
{
    public required Guid Device { get; init; }
    public required bool Online { get; init; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion? FirmwareVersion { get; init; }
}