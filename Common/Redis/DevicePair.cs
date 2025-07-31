using Redis.OM.Modeling;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "device-pair")]
public sealed class DevicePair
{
    [RedisIdField] [Indexed(IndexEmptyAndMissing = false)] public required Guid Id { get; set; }
    [Indexed(IndexEmptyAndMissing = false)] public required string PairCode { get; set; }
}