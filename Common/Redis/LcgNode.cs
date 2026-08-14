using Redis.OM.Modeling;

namespace OpenShock.Common.Redis;

[Document(StorageType = StorageType.Json, IndexName = "lcg-online-v5")]
public sealed class LcgNode
{
    /// <summary>
    /// Composite public node id (the Redis key): the gateway's full public base as
    /// <c>host[:port][/path]</c>. Port and path are only present when they differ from the
    /// defaults (443 / root), so a default-layout gateway keeps the bare-host id it always had.
    /// </summary>
    [RedisIdField] public required string Id { get; set; }

    /// <summary>Public host clients connect to.</summary>
    public required string Host { get; set; }

    /// <summary>Public port clients connect to (default 443).</summary>
    public required ushort Port { get; set; }

    /// <summary>Public path prefix the gateway is served under, e.g. "/gateway" (empty = root).</summary>
    public string PathPrefix { get; set; } = string.Empty;

    [Indexed(IndexEmptyAndMissing = false)] public required string Country { get; set; }
    [Indexed(Sortable = true, IndexEmptyAndMissing = false)] public required byte Load { get; set; }
    [Indexed(IndexEmptyAndMissing = false)] public string Environment { get; set; } = "Production";
}
