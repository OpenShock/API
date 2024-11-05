using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;
using Redis.OM.Modeling;
using Semver;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "device-online")]
public sealed class DeviceOnline
{
    [RedisIdField] [Indexed] public required Guid Id { get; set; }
    [Indexed] public required Guid Owner { get; set; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion FirmwareVersion { get; set; }
    public required string Gateway { get; set; }
    public required DateTimeOffset ConnectedAt { get; set; }
    public string? UserAgent { get; set; } = null;
    public TimeSpan? Uptime { get; set; }
    public TimeSpan? Latency { get; set; }
}