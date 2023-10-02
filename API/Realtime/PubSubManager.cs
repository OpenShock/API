using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Hubs;
using OpenShock.API.Models.WebSocket;
using OpenShock.API.Utils;
using OpenShock.Common;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.Device;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Utils;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using StackExchange.Redis;

#pragma warning disable CS8618

namespace OpenShock.API.Realtime;

public static class PubSubManager
{
    private static readonly ILogger Logger = ApplicationLogging.CreateLogger(typeof(PubSubManager));

    // DO NOT REMOVE THIS VARIABLE, IT WILL DISPOSE THE CONNECTION OTHERWISE
    private static ConnectionMultiplexer _con;
    private static IServiceProvider _serviceProvider;
    private static ISubscriber _subscriber;
    private static IRedisCollection<DeviceOnline> _devicesOnline;

    public static async Task Initialize(ConnectionMultiplexer con, IServiceProvider serviceProvider)
    {
        _con = con;
        _serviceProvider = serviceProvider;
        var provider = _serviceProvider.GetRequiredService<IRedisConnectionProvider>();
        _devicesOnline = provider.RedisCollection<DeviceOnline>(false);
        _subscriber = _con.GetSubscriber();
        await _subscriber.SubscribeAsync(
            new RedisChannel("__keyevent@0__:expired", RedisChannel.PatternMode.Literal),
            (_, message) => { LucTask.Run(() => RunLogic(message, false)); });
        await _subscriber.SubscribeAsync(
            new RedisChannel("__keyevent@0__:json.set", RedisChannel.PatternMode.Literal),
            (_, message) => { LucTask.Run(() => RunLogic(message, true)); });
        await _subscriber.SubscribeAsync(
            new RedisChannel("__keyevent@0__:del", RedisChannel.PatternMode.Literal),
            (_, message) => { LucTask.Run(() => RunLogic(message, false)); });

        await _subscriber.SubscribeAsync(new RedisChannel("msg-*", RedisChannel.PatternMode.Pattern), (channel, value) =>
        {
            switch (channel.ToString())
            {
                case "msg-device-control":
                    LucTask.Run(() => DeviceControl(value));
                    break;
                case "msg-device-control-captive":
                    LucTask.Run(() => DeviceControlCaptive(value));
                    break;
            }
        });
    }


    #region Exposing methods

    public static Task SendControlMessage(ControlMessage data) =>
        _subscriber.PublishAsync(new RedisChannel("msg-device-control", RedisChannel.PatternMode.Literal),
            JsonSerializer.Serialize(data));

    public static Task SendCaptiveControlMessage(CaptiveMessage data) =>
        _subscriber.PublishAsync(new RedisChannel("msg-device-control-captive", RedisChannel.PatternMode.Literal),
            JsonSerializer.Serialize(data));

    #endregion

    private static async Task DeviceControlCaptive(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<CaptiveMessage>(value.ToString());
        if (data == null) return;

        await WebsocketManager.DeviceWebSockets.SendMessageTo(data.DeviceId, new BaseResponse<ResponseType>
        {
            ResponseType = ResponseType.CaptiveControl,
            Data = data.Enabled
        });
    }

    private static async Task DeviceControl(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<ControlMessage>(value.ToString());
        if (data == null) return;

        foreach (var controlMessage in data.ControlMessages)
        {
            var shockies = controlMessage.Value.Select(shock => new ControlResponse
            {
                Id = shock.RfId, Duration = shock.Duration, Intensity = shock.Intensity, Type = shock.Type,
                Model = shock.Model
            });

            await WebsocketManager.DeviceWebSockets.SendMessageTo(controlMessage.Key, new BaseResponse<ResponseType>
            {
                ResponseType = ResponseType.Control,
                Data = shockies
            });
        }
    }

    private static async Task RunLogic(RedisValue message, bool set)
    {
        if (!message.HasValue) return;
        var msg = message.ToString().Split(':');
        if (msg.Length < 2) return;
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenShockContext>();
        var userHub = scope.ServiceProvider.GetRequiredService<IHubContext<UserHub, IUserHub>>();
        if (!Guid.TryParse(msg[1], out var id)) return;
        
        if (typeof(DeviceOnline).FullName == msg[0])
        {
            var data = await db.Devices.Where(x => x.Id == id).Select(x => new
            {
                x.Owner,
                SharedWith = x.Shockers.SelectMany(y => y.ShockerShares)
            }).SingleOrDefaultAsync();
            if (data == null) return;


            var sharedWith = await db.Users.Where(x => x.ShockerShares.Any(y => y.Shocker.Device == id))
                .Select(x => x.Id).ToArrayAsync();
            var userIds = new List<string>
            {
                "local#" + data.Owner
            };
            userIds.AddRange(sharedWith.Select(x => "local#" + x));
            var deviceOnline = await _devicesOnline.FindByIdAsync(msg[1]);
            var arr = new[]
            {
                new DeviceOnlineState
                {
                    Device = id,
                    Online = set,
                    FirmwareVersion = deviceOnline?.FirmwareVersion ?? null
                }
            };
            await userHub.Clients.Users(userIds).DeviceStatus(arr);
        }
    }
}