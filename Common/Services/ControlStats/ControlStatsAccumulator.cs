using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Services.ControlStats;

/// <summary>
/// Thread-safe in-memory buffer of <see cref="ControlStatDelta"/>s, merged per bucket so its size is bounded by the
/// number of distinct buckets touched rather than the number of recorded events.
/// </summary>
public sealed class ControlStatsAccumulator
{
    private readonly Lock _lock = new();
    private Dictionary<(Guid ShockerId, Guid? ControllerUserId, DateTime Bucket, ControlType Type, bool Live), ControlStatDelta> _pending = [];

    public void Add(ControlStatDelta delta)
    {
        if (delta.Type == ControlType.Stop) return;

        delta = delta with { Bucket = ControlStatDelta.ToBucket(delta.Bucket) };
        var key = (delta.ShockerId, delta.ControllerUserId, delta.Bucket, delta.Type, delta.Live);

        lock (_lock)
        {
            _pending[key] = _pending.TryGetValue(key, out var existing) ? existing.Merge(delta) : delta;
        }
    }

    public ControlStatDelta[] DequeueAll()
    {
        Dictionary<(Guid, Guid?, DateTime, ControlType, bool), ControlStatDelta> items;
        lock (_lock)
        {
            if (_pending.Count == 0) return [];
            items = _pending;
            _pending = [];
        }

        return items.Values.ToArray();
    }
}
