using OpenShock.Common.Models.WebSocket;

namespace OpenShock.API.Controller;

public interface IWebsocketController<T> where T : Enum
{
    public Guid Id { get; }
    public ValueTask QueueMessage(IBaseResponse<T> data);
    
}