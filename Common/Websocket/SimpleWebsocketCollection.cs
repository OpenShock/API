using OpenShock.Common.Extensions;

namespace OpenShock.Common.Websocket;

public sealed class SimpleWebsocketCollection<T, TR> where T : class, IWebsocketController<TR>
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
    private readonly Dictionary<Guid, List<T>> _websockets = [];

    public async Task RegisterConnection(T controller)
    {
        using (await _semaphore.LockAsyncScoped())
        {
            if (!_websockets.TryGetValue(controller.Id, out var list))
            {
                list = [controller];
                _websockets.Add(controller.Id, list);
            }

            list.Add(controller);
        }
    }

    public async Task<bool> UnregisterConnection(T controller)
    {
        using (await _semaphore.LockAsyncScoped())
        {
            if (!_websockets.TryGetValue(controller.Id, out var list)) return false;
            if (!list.Remove(controller)) return false;
            if (list.Count == 0)
            {
                _websockets.Remove(controller.Id);
            }
        }

        return true;
    }

    public async Task<T[]> GetConnections(Guid id)
    {
        using (await _semaphore.LockAsyncScoped())
        {
            if (!_websockets.TryGetValue(id, out var list)) return [];
            return list.ToArray();
        }
    }

    public async Task<uint> GetCount()
    {
        using (await _semaphore.LockAsyncScoped())
        {
            return (uint)_websockets.Sum(x => x.Value.Count);
        }
    }
}