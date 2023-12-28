using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.Device;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization;
using OpenShock.Serialization.Types;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Services.RedisPubSub;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace OpenShock.LiveControlGateway.PubSub;

/// <summary>
/// Redis subscription service, which handles listening to pub sub on redis
/// </summary>
public class RedisSubscriberService : IHostedService, IAsyncDisposable
{
    private readonly ISubscriber _subscriber;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="connectionMultiplexer"></param>
    public RedisSubscriberService(
        IConnectionMultiplexer connectionMultiplexer)
    {
        _subscriber = connectionMultiplexer.GetSubscriber();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _subscriber.SubscribeAsync(RedisChannels.DeviceControl, (_, message) => { LucTask.Run(() => DeviceControl(message)); });
        await _subscriber.SubscribeAsync(RedisChannels.DeviceCaptive, (_, message) => { LucTask.Run(() => DeviceControlCaptive(message)); });
        await _subscriber.SubscribeAsync(RedisChannels.DeviceUpdate, (_, message) => { LucTask.Run(() => DeviceUpdate(message)); });
    }

    private static async Task DeviceControl(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<ControlMessage>(value.ToString());
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
    
    private static async Task DeviceControlCaptive(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<CaptiveMessage>(value.ToString());
        if (data == null) return;

        await WebsocketManager.ServerToDevice.SendMessageTo(data.DeviceId, new ServerToDeviceMessage
        {
            Payload = new ServerToDeviceMessagePayload
            {
                CaptivePortalConfig = { Enabled = data.Enabled }
            }
        });
    }
    
    /// <summary>
    /// Update the device connection if found
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static Task DeviceUpdate(RedisValue value)
    {
        if (!value.HasValue) return Task.CompletedTask;
        var data = JsonSerializer.Deserialize<DeviceUpdatedMessage>(value.ToString());
        return data == null ? Task.CompletedTask : DeviceLifetimeManager.UpdateDevice(data.Id);
    }

    
    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _subscriber.UnsubscribeAllAsync();
        GC.SuppressFinalize(this);
    }

    ~RedisSubscriberService()
    {
        DisposeAsync().AsTask().Wait();
    }
}