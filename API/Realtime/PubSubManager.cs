using System.Text.Json;
using Redis.OM.Contracts;
using ShockLink.API.Realtime;
using ShockLink.API.Utils;
using ShockLink.Common.Models.WebSocket;
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

    public static void Initialize(ConnectionMultiplexer con, IServiceProvider serviceProvider)
    {
        _con = con;
        _serviceProvider = serviceProvider;
        var provider = _serviceProvider.GetRequiredService<IRedisConnectionProvider>();
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
            }
        });
    }


    #region Exposing methods

    public static Task SendControlMessage(ControlMessage data) =>
        _subscriber.PublishAsync("msg-device-control", JsonSerializer.Serialize(data));

    #endregion

    private static async Task DeviceControl(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<ControlMessage>(value.ToString());
        if (data == null) return;

        foreach (var controlMessage in data.ControlMessages)
        {
            var shockies = controlMessage.Shocks.Select(shock => new ControlResponse
                { Id = shock.RfId, Duration = shock.Duration, Intensity = shock.Intensity, Type = shock.Type });

            await WebsocketManager.DeviceWebSockets.SendMessageTo(controlMessage.DeviceId, new BaseResponse
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
        var id = msg[1];
        switch (msg[0])
        {
        }
    }
}