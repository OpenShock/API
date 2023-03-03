using ShockLink.Common.Models.WebSocket;

namespace ShockLink.API.Controller;

public interface IWebsocketController<T> where T : Enum
{
    public Guid Id { get; }
    public ValueTask QueueMessage(IBaseResponse<T> data);
    
}