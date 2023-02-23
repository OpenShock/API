using ShockLink.Common.Models.WebSocket;

namespace ShockLink.API.Controller;

public interface IWebsocketController<T> where T : Enum
{
    public string Id { get; }
    public ValueTask QueueMessage(IBaseResponse<T> data);
    
}