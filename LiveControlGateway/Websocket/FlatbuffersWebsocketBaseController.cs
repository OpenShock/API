using FlatSharp;
using OpenShock.ServicesCommon.Websocket;
using System.Net.WebSockets;

namespace OpenShock.LiveControlGateway.Websocket;

/// <summary>
/// Base for a flat buffers serialized websocket controller
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class FlatbuffersWebsocketBaseController<T> : WebsocketBaseController<T> where T : class, IFlatBufferSerializable
{
    /// <summary>
    /// The flat buffer serializer for the type we are serializing
    /// </summary>
    private readonly ISerializer<T> _flatBuffersSerializer;

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    /// <param name="flatBuffersSerializer"></param>
    public FlatbuffersWebsocketBaseController(ILogger<FlatbuffersWebsocketBaseController<T>> logger,
        IHostApplicationLifetime lifetime, ISerializer<T> flatBuffersSerializer) : base(logger, lifetime)
    {
        _flatBuffersSerializer = flatBuffersSerializer;
    }

    /// <inheritdoc />
    protected override Task SendWebSocketMessage(T message, WebSocket websocket, CancellationToken cancellationToken) =>
        FlatbufferWebSocketUtils.SendFullMessage(message, _flatBuffersSerializer, websocket, cancellationToken);

}