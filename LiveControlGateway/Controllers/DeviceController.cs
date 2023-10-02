using System.Net.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Communication with the devices aka ESP-32 micro controllers
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.DeviceToken)]
[Route("/{version:apiVersion}/ws/device")]
public sealed class DeviceController : WebsocketBaseController<ServerToDeviceMessage>
{
    private Common.OpenShockDb.Device _currentDevice = null!;

    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _currentDevice = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<Common.OpenShockDb.Device>>()
            .CurrentClient;
        base.OnActionExecuting(context);
    }

    /// <inheritdoc />
    public override Guid Id => _currentDevice.Id;
    
    
    /// <summary>
    /// DI
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    public DeviceController(ILogger<DeviceController> logger, IHostApplicationLifetime lifetime)
        : base(logger, lifetime, ServerToDeviceMessage.Serializer)
    {
    }

    /// <inheritdoc />
    protected override async Task Logic()
    {
        ValueWebSocketReceiveResult? result = null;
        do
        {
            try
            {
                if (WebSocket!.State == WebSocketState.Aborted) return;
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
        } while (result != null && result.Value.MessageType != WebSocketMessageType.Close);

        Close.Cancel();
    }

    /// <inheritdoc />
    protected override void RegisterConnection()
    {
        WebsocketManager.ServerToDevice.RegisterConnection(this);
    }

    /// <inheritdoc />
    protected override void UnregisterConnection()
    {
        WebsocketManager.ServerToDevice.UnregisterConnection(this);
    }
}