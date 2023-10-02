using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization;

namespace OpenShock.LiveControlGateway.Controllers;

[ApiController]
//[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.DeviceToken)]
[Route("/{version:apiVersion}/ws/device")]
public sealed class DeviceController : WebsocketBaseController<ServerToDeviceMessage>
{
    public DeviceController(ILogger<WebsocketBaseController<ServerToDeviceMessage>> logger, IHostApplicationLifetime lifetime)
        : base(logger, lifetime, ServerToDeviceMessage.Serializer)
    {
    }

    public override Guid Id { get; }
    protected override async Task Logic()
    {
        ValueWebSocketReceiveResult? result = null;
        do
        {
            try
            {
                if (WebSocket.State == WebSocketState.Aborted) return;
                var message =
                    await WebSocketUtils.ReceiveFullMessageAsyncNonAlloc(WebSocket, _flatBuffersSerializer, Linked.Token);
                result = message.Item1;

                if (result.Value.MessageType == WebSocketMessageType.Close && WebSocket.State == WebSocketState.Open)
                {
                    //await SelfOffline();
                    try
                    {
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal close", Linked.Token);
                    }
                    catch (OperationCanceledException e)
                    {
                        Logger.LogError(e, "Error during close handshake");
                    }

                    Close.Cancel();
                    Logger.LogInformation("Closing websocket connection");
                    return;
                }

                var json = message.Item2;
                if (json == null) continue;
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("WebSocket connection terminated due to close or shutdown");
                Close.Cancel();
                return;
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
            Console.WriteLine(result == null);
        } while (result != null && result.Value.MessageType != WebSocketMessageType.Close);

        Close.Cancel();
    }

    protected override void RegisterConnection()
    {
        WebsocketManager.ServerToDevice.RegisterConnection(this);

        QueueMessage(new ServerToDeviceMessage()
        {
            Payload = new ServerToDeviceMessagePayload(new ShockerCommandList()
            {
                Commands = new List<ShockerCommand>()
                {
                    new ShockerCommand()
                    {
                        Id = 435,
                        Duration = 34,
                        Intensity = 33,
                        Model = 1,
                        Type = ShockerCommandType.Shock
                    }
                }
            })
        });
    }
    
    protected override void UnregisterConnection()
    {
        WebsocketManager.ServerToDevice.UnregisterConnection(this);
    }
}