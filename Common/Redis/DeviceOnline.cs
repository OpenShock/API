using System.Text.Json.Serialization;
using OpenShock.Common.Serialization;
using Redis.OM.Modeling;
using Semver;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "device-online")]
public class DeviceOnline
{
    [RedisIdField] [Indexed] public required Guid Id { get; set; }
    [Indexed] public required Guid Owner { get; set; }
    [JsonConverter(typeof(SemVersionJsonConverter))]
    public SemVersion? FirmwareVersion { get; set; }
    public string? Gateway { get; set; }
}