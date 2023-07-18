﻿using System.Net.WebSockets;
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

    private Device _currentDevice = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _currentDevice = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<Device>>().CurrentClient;
        base.OnActionExecuting(context);
    }
    
    public override Guid Id => _currentDevice.Id;
    
    public DeviceWebSocketController(ILogger<DeviceWebSocketController> logger, IHostApplicationLifetime lifetime, IRedisConnectionProvider redisConnectionProvider) : base(logger, lifetime)
    {
        _redis = redisConnectionProvider;
        _devicesOnline = redisConnectionProvider.RedisCollection<DeviceOnline>(false);
        Console.WriteLine("FirmwareVersion: " + HttpContext.Request.Headers["FirmwareVersion"]);
    }
    
    protected override void RegisterConnection()
    {
        WebsocketManager.DeviceWebSockets.RegisterConnection(this);
    }

    protected override void UnregisterConnection()
    {
        WebsocketManager.DeviceWebSockets.UnregisterConnection(this);
    }

    protected override async Task Logic()
    {
        WebSocketReceiveResult? result = null;
        do
        {
            try
            {
                if (WebSocket.State == WebSocketState.Aborted) return;
                var message = await WebSocketUtils.ReceiveFullMessage(WebSocket, Linked.Token);
                result = message.Item1;

                if (result.MessageType == WebSocketMessageType.Close || result.CloseStatus.HasValue)
                {
                    //await SelfOffline();
                    try
                    {
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                            result.CloseStatusDescription ?? "Normal close", Linked.Token);
                    }
                    catch (OperationCanceledException e)
                    {
                        Logger.LogError(e, "Error during close handshake");
                    }

                    Close.Cancel();
                    Logger.LogInformation("Closing websocket connection");
                    return;
                }

                var msg = Encoding.UTF8.GetString(message.Item2.ToArray());
                var json = JsonConvert.DeserializeObject<BaseRequest<RequestType>>(msg);
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
        } while (result is { CloseStatus: null });

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
        var online = await _devicesOnline.FindByIdAsync(_currentDevice.Id.ToString());
        if (online == null)
        {
            await _devicesOnline.InsertAsync(new DeviceOnline
            {
                Id = _currentDevice.Id,
                Owner = _currentDevice.Owner
            }, TimeSpan.FromSeconds(65));
            return;
        }
        
        await _redis.Connection.ExecuteAsync("EXPIRE",
                $"ShockLink.Common.Redis.DeviceOnline:{_currentDevice.Id}", "65");
    }

}