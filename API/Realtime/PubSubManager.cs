using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using ShockLink.API.Models.WebSocket;
using ShockLink.API.Realtime;
using ShockLink.API.Utils;
using ShockLink.Common.Models.WebSocket;
using ShockLink.Common.Models.WebSocket.Device;
using ShockLink.Common.Redis;
using ShockLink.Common.Redis.PubSub;
using ShockLink.Common.ShockLinkDb;
using StackExchange.Redis;

#pragma warning disable CS8618

namespace ShockLink.API.RedisPubSub;

public static class PubSubManager
{
    private static readonly ILogger Logger = ApplicationLogging.CreateLogger(typeof(PubSubManager));

    // DO NOT REMOVE THIS VARIABLE, IT WILL DISPOSE THE CONNECTION OTHERWISE
    private static ConnectionMultiplexer _con;
    private static IServiceProvider _serviceProvider;
    private static ISubscriber _subscriber;
    private static IRedisCollection<DeviceOnline> _devicesOnline;

    public static void Initialize(ConnectionMultiplexer con, IServiceProvider serviceProvider)
    {
        _con = con;
        _serviceProvider = serviceProvider;
        var provider = _serviceProvider.GetRequiredService<IRedisConnectionProvider>();
        _devicesOnline = provider.RedisCollection<DeviceOnline>(false);
        _subscriber = _con.GetSubscriber();
        _subscriber.Subscribe(
            new RedisChannel("__keyevent@0__:expired", RedisChannel.PatternMode.Literal),
            (_, message) => { LucTask.Run(() => RunLogic(message, false)); });
        _subscriber.Subscribe(
            new RedisChannel("__keyevent@0__:json.set", RedisChannel.PatternMode.Literal),
            (_, message) => { LucTask.Run(() => RunLogic(message, true)); });
        _subscriber.Subscribe(
            new RedisChannel("__keyevent@0__:del", RedisChannel.PatternMode.Literal),
            (_, message) => { LucTask.Run(() => RunLogic(message, false)); });

        _subscriber.Subscribe(new RedisChannel("msg-*", RedisChannel.PatternMode.Pattern), (channel, value) =>
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
        _subscriber.PublishAsync("msg-device-control", JsonSerializer.Serialize(data));
    
    public static Task SendCaptiveControlMessage(CaptiveMessage data) =>
        _subscriber.PublishAsync("msg-device-control-captive", JsonSerializer.Serialize(data));

    #endregion

    private static async Task DeviceControlCaptive(RedisValue value)
    {
        if(!value.HasValue) return;
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
                { Id = shock.RfId, Duration = shock.Duration, Intensity = shock.Intensity, Type = shock.Type });

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
        await using var db = scope.ServiceProvider.GetRequiredService<ShockLinkContext>();
        if (!Guid.TryParse(msg[1], out var id)) return;
        switch (msg[0])
        {
            case "ShockLink.Common.Redis.DeviceOnline":
                var data = await db.Devices.Where(x => x.Id == id).Select(x => new
                {
                    x.Owner,
                    SharedWith = x.Shockers.SelectMany(y => y.ShockerShares)
                }).SingleOrDefaultAsync();
                if (data == null) return;


                var wsData = new BaseResponse<Common.Models.WebSocket.User.ResponseType>
                {
                    ResponseType = Common.Models.WebSocket.User.ResponseType.DeviceOnlineState,
                    Data = new List<DeviceOnlineState>
                    {
                        new()
                        {
                            Device = id,
                            Online = set
                        }
                    }
                };
                await WebsocketManager.UserWebSockets.SendMessageTo(data.Owner, wsData);
                
                var sharedWith = await db.Users.Where(x => x.ShockerShares.Any(y => y.Shocker.Device == id))
                    .Select(x => x.Id).ToArrayAsync();
                await WebsocketManager.UserWebSockets.SendMessageTo(sharedWith, wsData);
                break;
        }
    }
}