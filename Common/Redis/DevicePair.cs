using Redis.OM.Modeling;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "device-pair")]
public class DevicePair
{
    [RedisIdField] [Indexed] public required Guid Id { get; set; }
    [Indexed] public required string PairCode { get; set; }
}