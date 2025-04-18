using System.Net.WebSockets;
using FlatSharp;
using OpenShock.Common.Websocket;

namespace OpenShock.LiveControlGateway.Websocket;

/// <summary>
/// Base for a flat buffers serialized websocket controller
/// </summary>
/// <typeparam name="TIn">The type we are receiving / deserializing</typeparam>
/// <typeparam name="TOut">The type we are sending out / serializing</typeparam>
public abstract class FlatbuffersWebsocketBaseController<TIn, TOut> : WebsocketBaseController<TOut> where TIn : class, IFlatBufferSerializable where TOut : class, IFlatBufferSerializable
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
    /// <param name="lifetime"></param>
    /// <param name="incomingSerializer"></param>
    /// <param name="outgoingSerializer"></param>
    public FlatbuffersWebsocketBaseController(ILogger<FlatbuffersWebsocketBaseController<TIn, TOut>> logger,
        IHostApplicationLifetime lifetime, ISerializer<TIn> incomingSerializer, ISerializer<TOut> outgoingSerializer) : base(logger, lifetime)
    {
        _incomingSerializer = incomingSerializer;
        _outgoingSerializer = outgoingSerializer;
    }

    /// <inheritdoc />
    protected override Task SendWebSocketMessage(TOut message, WebSocket websocket, CancellationToken cancellationToken) =>
        FlatbufferWebSocketUtils.SendFullMessage(message, _outgoingSerializer, websocket, cancellationToken);
    
    /// <summary>
    /// Handle the incoming message
    /// </summary>
    /// <param name="data"></param>
    /// <returns>false if message is invalid</returns>
    protected abstract Task<bool> Handle(TIn data);
    
    /// <inheritdoc />
    protected override async Task Logic()
    {
        try
        {
            while (!LinkedToken.IsCancellationRequested)
            {
                if (WebSocket is null or { State: WebSocketState.Aborted }) return;
                var message =
                    await FlatbufferWebSocketUtils.ReceiveFullMessageAsyncNonAlloc(WebSocket,
                        _incomingSerializer, LinkedToken);
                
                
                bool ok = await message.Match(
                    Handle,
                    async _ =>
                    {
                        await WebSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, "Invalid flatbuffers message", LinkedToken);
                        return false;
                    },
                    async _ =>
                    {
                        if (WebSocket.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
                        {
                            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal close", LinkedToken);
                        }

                        Logger.LogInformation("Closing websocket connection");
                        return false;
                    });

                if (!ok) break;
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("WebSocket connection terminated due to close or shutdown");
        }
        catch (WebSocketException e)
        {
            if (e.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
            {
                Logger.LogError(e, "Error in receive loop, websocket exception");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception while processing websocket request");
        }
    }
}