using Redis.OM.Modeling;

namespace ShockLink.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "device-online")]
public class DeviceOnline
{
    [RedisIdField] [Indexed] public required Guid Id { get; set; }
    [Indexed] public required Guid Owner { get; set; }
}