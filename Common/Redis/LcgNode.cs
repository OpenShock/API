using Redis.OM.Modeling;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "lcg-online-v4")]
public sealed class LcgNode
{
    [RedisIdField] [Indexed(IndexEmptyAndMissing = false)] public required string Fqdn { get; set; }
    [Indexed(IndexEmptyAndMissing = false)] public required string Country { get; set; }
    [Indexed(Sortable = true, IndexEmptyAndMissing = false)] public required byte Load { get; set; }
    [Indexed(IndexEmptyAndMissing = false)] public string Environment { get; set; } = "Production";
    
}