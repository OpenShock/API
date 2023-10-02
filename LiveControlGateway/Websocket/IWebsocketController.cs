using FlatSharp;

namespace OpenShock.LiveControlGateway.Websocket;

public interface IWebsocketController<in T> where T : class, IFlatBufferSerializable
{
    public Guid Id { get; }
    public ValueTask QueueMessage(T data);
}