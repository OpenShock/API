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
    [RedisIdField] public required string Id { get; init; }

    /// <summary>Public host clients connect to.</summary>
    public required string Host { get; init; }

    /// <summary>Public port clients connect to (default 443).</summary>
    public required ushort Port { get; init; }

    /// <summary>Public path prefix the gateway is served under, e.g. "/gateway" (empty = root).</summary>
    public string PathPrefix { get; init; } = string.Empty;

    [Indexed(IndexEmptyAndMissing = false)] public required string Country { get; init; }
    [Indexed(Sortable = true, IndexEmptyAndMissing = false)] public required byte Load { get; set; }

    /// <summary>Configured gateway location, or null when only <see cref="Country"/> is known.</summary>
    public double? Latitude { get; init; }

    /// <summary>See <see cref="Latitude"/>.</summary>
    public double? Longitude { get; init; }
    [Indexed(IndexEmptyAndMissing = false)] public string Environment { get; init; } = "Production";
}
