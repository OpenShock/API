using Redis.OM.Modeling;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "lcg-online")]
public class LcgNode
{
    [RedisIdField] [Indexed] public required string Fqdn { get; set; }
    [Indexed] public required string Country { get; set; }
    public required byte Load { get; set; }
}