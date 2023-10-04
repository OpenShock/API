using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.Device;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization;
using OpenShock.Serialization.Types;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using StackExchange.Redis;

#pragma warning disable CS8618

namespace OpenShock.LiveControlGateway.PubSub;

/// <summary>
/// Redis Pub/Sub
/// </summary>
public static class PubSubManager
{
    private static readonly ILogger Logger = ApplicationLogging.CreateLogger(typeof(PubSubManager));

    // DO NOT REMOVE THIS VARIABLE, IT WILL DISPOSE THE CONNECTION OTHERWISE
    private static ConnectionMultiplexer _con;
    private static ISubscriber _subscriber;

    /// <summary>
    /// Called on application start to init the subscribers
    /// </summary>
    /// <param name="con"></param>
    public static async Task Initialize(ConnectionMultiplexer con)
    {
        _con = con;
        _subscriber = _con.GetSubscriber();

        // Setup subscription channels (sync since we are doing this on app start)
        await _subscriber.SubscribeAsync(new RedisChannel("msg-device-control", RedisChannel.PatternMode.Literal),
            (channel, value) => { LucTask.Run(() => DeviceControl(value)); });

        await _subscriber.SubscribeAsync(
            new RedisChannel("msg-device-control-captive", RedisChannel.PatternMode.Literal),
            (channel, value) => { LucTask.Run(() => DeviceControlCaptive(value)); });
    }

    private static async Task DeviceControlCaptive(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<CaptiveMessage>(value.ToString()!);
        if (data == null) return;

        await WebsocketManager.ServerToDevice.SendMessageTo(data.DeviceId, new ServerToDeviceMessage
        {
            Payload = new ServerToDeviceMessagePayload
            {
                CaptivePortalConfig = { Enabled = data.Enabled }
            }
        });
    }

    private static async Task DeviceControl(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<ControlMessage>(value.ToString()!);
        if (data == null) return;

        foreach (var controlMessage in data.ControlMessages)
        {
            var shockies = controlMessage.Value.Select(shock => new ShockerCommand
            {
                Id = shock.RfId, Duration = shock.Duration, Intensity = shock.Intensity,
                Type = (ShockerCommandType)shock.Type,
                Model = (ShockerModelType)shock.Model
            }).ToList();
            
            await WebsocketManager.ServerToDevice.SendMessageTo(controlMessage.Key, new ServerToDeviceMessage()
            {
                Payload = new ServerToDeviceMessagePayload(new ShockerCommandList { Commands = shockies })
            });
        }
    }
}