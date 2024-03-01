using System.Collections.Concurrent;

namespace OpenShock.ServicesCommon.Websocket;

public class SimpleWebsocketCollection<T, TR> where T : class, IWebsocketController<TR>
{
    private readonly ConcurrentDictionary<Guid, List<T>> _websockets = new();

    public void RegisterConnection(T controller)
    {
        var list = _websockets.GetOrAdd(controller.Id,
            new List<T> { controller });
        lock (list)
        {
            if (!list.Contains(controller)) list.Add(controller);
        }
    }

    public void UnregisterConnection(T controller)
    {
        var key = controller.Id;
        if (!_websockets.TryGetValue(key, out var list)) return;

        lock (list)
        {
            list.Remove(controller);
            if (list.Count <= 0) _websockets.TryRemove(key, out _);
        }
    }

    public bool IsConnected(Guid id) => _websockets.ContainsKey(id);

    public IList<T> GetConnections(Guid id)
    {
        if (_websockets.TryGetValue(id, out var list))
            return list;
        return [];
    }

    public async ValueTask SendMessageTo(Guid id, TR msg)
    {
        var list = GetConnections(id);

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < list.Count; i++)
        {
            var conn = list[i];
            await conn.QueueMessage(msg);
        }
    }

    public Task SendMessageTo(TR msg, params Guid[] id) => SendMessageTo(id, msg);

    public Task SendMessageTo(IEnumerable<Guid> id, TR msg)
    {
        var tasks = id.Select(x => SendMessageTo(x, msg).AsTask());
        return Task.WhenAll(tasks);
    }

    public async ValueTask SendMessageToAll(TR msg)
    {
        // Im cloning a moment-in-time snapshot on purpose here, so we dont miss any connections.
        // This is fine since this is not regularly called, and does not need to be realtime.
        foreach (var (_, list) in _websockets.ToArray())
        foreach (var websocketController in list)
            await websocketController.QueueMessage(msg);
    }

    public IEnumerable<T> GetConnectedById(IEnumerable<Guid> ids)
    {
        List<T> found = [];
        foreach (var id in ids) found.AddRange(GetConnections(id));
        return found;
    }

    public uint Count => (uint)_websockets.Sum(x => x.Value.Count);
}