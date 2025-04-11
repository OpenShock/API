﻿using Redis.OM.Modeling;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "lcg-online-v4")]
public sealed class LcgNode
{
    [RedisIdField] [Indexed] public required string Fqdn { get; set; }
    [Indexed] public required string Country { get; set; }
    [Indexed(Sortable = true)] public required byte Load { get; set; }
    [Indexed] public string Environment { get; set; } = "Production";
    
}