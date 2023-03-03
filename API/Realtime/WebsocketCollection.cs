using System.Collections.Concurrent;
using ShockLink.API.Controller;
using ShockLink.Common.Models.WebSocket;

namespace ShockLink.API.Realtime;

public class WebsocketCollection<T> where T : Enum
{
    private readonly ConcurrentDictionary<Guid, List<IWebsocketController<T>>> _websockets = new();

    public void RegisterConnection(IWebsocketController<T> controller)
    {
        var list = _websockets.GetOrAdd(controller.Id,
            new List<IWebsocketController<T>> { controller });
        lock (list)
        {
            if (!list.Contains(controller)) list.Add(controller);
        }
    }

    public void UnregisterConnection(IWebsocketController<T> controller)
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

    public IList<IWebsocketController<T>> GetConnections(Guid id)
    {
        if (_websockets.TryGetValue(id, out var list))
            return list;
        return Array.Empty<IWebsocketController<T>>();
    }

    public async ValueTask SendMessageTo(Guid id, IBaseResponse<T> msg)
    {
        var list = GetConnections(id);

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < list.Count; i++)
        {
            var conn = list[i];
            await conn.QueueMessage(msg);
        }
    }

    public Task SendMessageTo(IBaseResponse<T> msg, params Guid[] id) => SendMessageTo(id, msg);

    public Task SendMessageTo(IEnumerable<Guid> id, IBaseResponse<T> msg)
    {
        var tasks = id.Select(x => SendMessageTo(x, msg).AsTask());
        return Task.WhenAll(tasks);
    }

    public async ValueTask SendMessageToAll(IBaseResponse<T> msg)
    {
        // Im cloning a moment-in-time snapshot on purpose here, so we dont miss any connections.
        // This is fine since this is not regularly called, and does not need to be realtime.
        foreach (var (_, list) in _websockets.ToArray())
        foreach (var websocketController in list)
            await websocketController.QueueMessage(msg);
    }

    public IEnumerable<IWebsocketController<T>> GetConnectedById(IEnumerable<Guid> ids)
    {
        var found = new List<IWebsocketController<T>>();
        foreach (var id in ids) found.AddRange(GetConnections(id));
        return found;
    }

    public uint Count => (uint)_websockets.Sum(x => x.Value.Count);
}