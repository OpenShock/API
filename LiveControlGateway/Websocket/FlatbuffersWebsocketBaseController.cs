using System.Net.WebSockets;
using FlatSharp;
using OpenShock.Common.Websocket;

namespace OpenShock.LiveControlGateway.Websocket;

/// <summary>
/// Base for a flat buffers serialized websocket controller
/// </summary>
/// <typeparam name="TIn">The type we are receiving / deserializing</typeparam>
/// <typeparam name="TOut">The type we are sending out / serializing</typeparam>
public abstract class FlatbuffersWebsocketBaseController<TIn, TOut> : WebsocketBaseController<TOut>
    where TIn : class, IFlatBufferSerializable where TOut : class, IFlatBufferSerializable
{
    /// <summary>
    /// The flat buffer serializer for the type we are deserializing
    /// </summary>
    private readonly ISerializer<TIn> _incomingSerializer;

    /// <summary>
    /// The flat buffer serializer for the type we are serializing
    /// </summary>
    private readonly ISerializer<TOut> _outgoingSerializer;

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="incomingSerializer"></param>
    /// <param name="outgoingSerializer"></param>
    protected FlatbuffersWebsocketBaseController(ILogger<FlatbuffersWebsocketBaseController<TIn, TOut>> logger,
        ISerializer<TIn> incomingSerializer, ISerializer<TOut> outgoingSerializer) : base(logger)
    {
        _incomingSerializer = incomingSerializer;
        _outgoingSerializer = outgoingSerializer;
    }

    /// <inheritdoc />
    protected override Task
        SendWebSocketMessage(TOut message, WebSocket websocket, CancellationToken cancellationToken) =>
        FlatbufferWebSocketUtils.SendFullMessage(message, _outgoingSerializer, websocket, cancellationToken);

    /// <summary>
    /// Handle the incoming message
    /// </summary>
    /// <param name="data"></param>
    /// <returns>false if message is invalid</returns>
    protected abstract Task<bool> Handle(TIn data);

    /// <inheritdoc />
    protected override async Task<bool> HandleReceive(CancellationToken cancellationToken)
    {
        var message =
            await FlatbufferWebSocketUtils.ReceiveFullMessageAsyncNonAlloc(WebSocket!,
                _incomingSerializer, cancellationToken);
        
        var continueLoop = await message.Match(
            Handle,
            async _ =>
            {
                await ForceClose(WebSocketCloseStatus.InvalidPayloadData, "Invalid flatbuffers message");
                return false;
            },
            _ =>
            {
                Logger.LogTrace("Client sent closure");
                return Task.FromResult(false);
            });

        return continueLoop;
    }
}