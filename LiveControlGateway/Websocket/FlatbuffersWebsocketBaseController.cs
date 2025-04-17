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
    /// <returns></returns>
    protected abstract Task Handle(TIn data);
    
    /// <inheritdoc />
    protected override async Task Logic()
    {
        while (!LinkedToken.IsCancellationRequested)
        {
            try
            {
                if (WebSocket is null or { State: WebSocketState.Aborted }) return;
                var message =
                    await FlatbufferWebSocketUtils.ReceiveFullMessageAsyncNonAlloc(WebSocket,
                        _incomingSerializer, LinkedToken);

                // All is good, normal message, deserialize and handle
                if (message.IsT0)
                {
                    try
                    {
                        var serverMessage = message.AsT0;
                        await Handle(serverMessage);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Error while handling device message");
                    }
                }

                // Deserialization failed, log and continue
                else if (message.IsT1)
                {
                    Logger.LogWarning(message.AsT1.Exception, "Deserialization failed for websocket message");
                }

                // Device sent closure, close connection
                else if (message.IsT2)
                {
                    if (WebSocket.State != WebSocketState.Open)
                    {
                        Logger.LogTrace("Client sent closure, but connection state is not open");
                        break;
                    }

                    try
                    {
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal close",
                            LinkedToken);
                    }
                    catch (OperationCanceledException e)
                    {
                        Logger.LogError(e, "Error during close handshake");
                    }

                    Logger.LogInformation("Closing websocket connection");
                    break;
                }
                else
                {
                    throw new NotImplementedException(); // message.T? is not implemented
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("WebSocket connection terminated due to close or shutdown");
                break;
            }
            catch (WebSocketException e)
            {
                if (e.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
                    Logger.LogError(e, "Error in receive loop, websocket exception");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception while processing websocket request");
            }
        }

        await Close.CancelAsync();
    }
    
}