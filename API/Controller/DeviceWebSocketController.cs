using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using ShockLink.API.Authentication;
using ShockLink.API.Realtime;
using ShockLink.API.Utils;
using ShockLink.Common.Models.WebSocket;
using ShockLink.Common.Models.WebSocket.Device;
using ShockLink.Common.Redis;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller;

[ApiController]
[Authorize(AuthenticationSchemes = ShockLinkAuthSchemas.DeviceToken)]
[Route("/{version:apiVersion}/ws/device")]
public class DeviceWebSocketController : WebsocketControllerBase<ResponseType>
{
    private readonly IRedisCollection<DeviceOnline> _devicesOnline;
    private readonly IRedisConnectionProvider _redis;

    private Common.ShockLinkDb.Device _currentDevice = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _currentDevice = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<Common.ShockLinkDb.Device>>()
            .CurrentClient;
        base.OnActionExecuting(context);
    }

    public override Guid Id => _currentDevice.Id;

    public DeviceWebSocketController(ILogger<DeviceWebSocketController> logger, IHostApplicationLifetime lifetime,
        IRedisConnectionProvider redisConnectionProvider) : base(logger, lifetime)
    {
        _redis = redisConnectionProvider;
        _devicesOnline = redisConnectionProvider.RedisCollection<DeviceOnline>(false);
    }

    protected override void RegisterConnection()
    {
        if (HttpContext.Request.Headers.TryGetValue("FirmwareVersion", out var header) &&
            Version.TryParse(header, out var version)) FirmwareVersion = version;

        WebsocketManager.DeviceWebSockets.RegisterConnection(this);
    }

    protected override void UnregisterConnection()
    {
        WebsocketManager.DeviceWebSockets.UnregisterConnection(this);
    }

    protected override async Task Logic()
    {
        ValueWebSocketReceiveResult? result = null;
        do
        {
            try
            {
                if (WebSocket.State == WebSocketState.Aborted) return;
                var message =
                    await WebSocketUtils.ReceiveFullMessageAsyncNonAlloc<BaseRequest<RequestType>>(WebSocket,
                        Linked.Token);
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
                await ProcessResult(json);
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

    private async Task ProcessResult(BaseRequest<RequestType> json)
    {
        switch (json.RequestType)
        {
            case RequestType.KeepAlive:
                await SelfOnline();
                break;
        }
    }

    private async Task SelfOnline()
    {
        var deviceId = _currentDevice.Id.ToString();
        var online = await _devicesOnline.FindByIdAsync(deviceId);
        if (online == null)
        {
            await _devicesOnline.InsertAsync(new DeviceOnline
            {
                Id = _currentDevice.Id,
                Owner = _currentDevice.Owner,
                FirmwareVersion = FirmwareVersion
            }, TimeSpan.FromSeconds(65));
            return;
        }

        if (online.FirmwareVersion != FirmwareVersion)
        {
            var changeTracker = _redis.RedisCollection<DeviceOnline>();
            var trackedDevice = await changeTracker.FindByIdAsync(deviceId);
            if (trackedDevice != null)
            {
                trackedDevice.FirmwareVersion = FirmwareVersion;
                await changeTracker.SaveAsync();
                Logger.LogInformation("Updated firmware version of online device");
            }
            else Logger.LogWarning("Could not save changed firmware version to redis, device was not found in change tracker, this shouldn't be possible but it somehow was?");
        }

        await _redis.Connection.ExecuteAsync("EXPIRE",
            $"ShockLink.Common.Redis.DeviceOnline:{_currentDevice.Id}", "65");
    }

    private Version? FirmwareVersion { get; set; }
}