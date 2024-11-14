using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;
using Redis.OM.Modeling;
using Semver;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = IndexName)]
public sealed class DeviceOnline
{
    public const string IndexName = "device-online";
    
    [RedisIdField] [Indexed] public required Guid Id { get; set; }
    [Indexed] public required Guid Owner { get; set; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion FirmwareVersion { get; set; }
    public required string Gateway { get; set; }
    public required DateTimeOffset ConnectedAt { get; set; }
    public string? UserAgent { get; set; } = null;
    
    [JsonConverter(typeof(TimeSpanToMillisecondsConverter))]
    public TimeSpan? Uptime { get; set; }
    [JsonConverter(typeof(TimeSpanToMillisecondsConverter))]
    public TimeSpan? Latency { get; set; }
    public int Rssi { get; set; }
}