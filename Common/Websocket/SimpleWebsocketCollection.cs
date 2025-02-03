using System.Collections.Concurrent;

namespace OpenShock.Common.Websocket;

public sealed class SimpleWebsocketCollection<T, TR> where T : class, IWebsocketController<TR>
{
    private readonly ConcurrentDictionary<Guid, List<T>> _websockets = new();

    public void RegisterConnection(T controller)
    {
        var list = _websockets.GetOrAdd(controller.Id, [controller]);

        lock (list)
        {
            if (!list.Contains(controller)) list.Add(controller);
        }
    }

    public bool UnregisterConnection(T controller)
    {
        var key = controller.Id;
        if (!_websockets.TryGetValue(key, out var list)) return false;

        lock (list)
        {
            if (!list.Remove(controller)) return false;
            if (list.Count == 0)
            {
                _websockets.TryRemove(key, out _);
            }
        }

        return true;
    }

    public bool IsConnected(Guid id) => _websockets.ContainsKey(id);

    public T[] GetConnections(Guid id)
    {
        if (!_websockets.TryGetValue(id, out var list)) return [];

        lock (list)
        {
            return list.ToArray();
        }
    }

    public uint Count => (uint)_websockets.Sum(kvp => { lock (kvp.Value) { return kvp.Value.Count; } });
}