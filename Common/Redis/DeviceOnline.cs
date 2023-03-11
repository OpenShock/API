using Redis.OM.Modeling;

namespace ShockLink.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "device-online-2")]
public class DeviceOnline
{
    [RedisIdField] [Indexed] public required Guid Id { get; set; }
    [Indexed] public required string Owner { get; set; }
}