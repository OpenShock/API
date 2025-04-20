using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;
using Redis.OM.Modeling;
using Semver;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = IndexName)]
public sealed class DeviceOnline
{
    public const string IndexName = "device-online";
    
    [RedisIdField] [Indexed(IndexEmptyAndMissing = false)] public required Guid Id { get; set; }
    [Indexed(IndexEmptyAndMissing = false)] public required Guid Owner { get; set; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public required SemVersion FirmwareVersion { get; set; }
    public required string Gateway { get; set; }
    public required DateTimeOffset ConnectedAt { get; set; }
    public string? UserAgent { get; set; } = null;
    
    public DateTimeOffset BootedAt { get; set; }
    public ushort? LatencyMs { get; set; }
    public int? Rssi { get; set; }
}