using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using ShockLink.API.Authentication;
using ShockLink.API.Models.WebSocket;
using ShockLink.API.Realtime;
using ShockLink.API.RedisPubSub;
using ShockLink.API.Serialization;
using ShockLink.API.Utils;
using ShockLink.Common.Models.WebSocket;
using ShockLink.Common.Models.WebSocket.User;
using ShockLink.Common.Redis;
using ShockLink.Common.Redis.PubSub;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller;

[ApiController]
[Authorize(AuthenticationSchemes = ShockLinkAuthSchemas.SessionTokenCombo)]
[Route("/{version:apiVersion}/ws/user")]
public class UserWebSocketController : WebsocketControllerBase<ResponseType>
{
    private readonly IRedisCollection<DeviceOnline> _devicesOnline;
    private readonly IRedisConnectionProvider _redis;
    private readonly IServiceProvider _serviceProvider;

    private LinkUser _currentUser = null!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _currentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<LinkUser>>()
            .CurrentClient;
        base.OnActionExecuting(context);
    }

    public override Guid Id => _currentUser.DbUser.Id;

    public UserWebSocketController(ILogger<UserWebSocketController> logger, IHostApplicationLifetime lifetime,
        IRedisConnectionProvider redisConnectionProvider, IServiceProvider serviceProvider) : base(logger, lifetime)
    {
        _redis = redisConnectionProvider;
        _serviceProvider = serviceProvider;
        _devicesOnline = redisConnectionProvider.RedisCollection<DeviceOnline>(false);
    }

    protected override void RegisterConnection()
    {
        WebsocketManager.UserWebSockets.RegisterConnection(this);
    }

    protected override void UnregisterConnection()
    {
        WebsocketManager.UserWebSockets.UnregisterConnection(this);
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
                var json = msg.Deserialize<BaseRequest<RequestType>>();
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
            case RequestType.Control:
                var control = json.Data?.SlDeserialize<IEnumerable<Control>>();
                if (control == null) return;
                await Control(control);
                break;
        }
    }

    private async Task Control(IEnumerable<Control> shocks)
    {
        var finalMessages = new Dictionary<Guid, IList<ControlMessage.ShockerControlInfo>>();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShockLinkContext>();
        var ownShockers = await db.Shockers.Where(x => x.DeviceNavigation.Owner == _currentUser.DbUser.Id).Select(x =>
            new
            {
                x.Id,
                x.RfId,
                x.Device
            }).ToListAsync();

        var sharedShockers = await db.ShockerShares.Where(x => x.SharedWith == _currentUser.DbUser.Id).Select(x => new
        {
            x.Shocker.Id,
            x.Shocker.RfId,
            x.Shocker.Device
        }).ToListAsync();

        ownShockers.AddRange(sharedShockers);

        var curTime = DateTime.UtcNow;
        foreach (var shock in shocks.DistinctBy(x => x.Id))
        {
            var shockerInfo = ownShockers.FirstOrDefault(x => x.Id == shock.Id);
            if (shockerInfo == null)
            {
                Logger.LogWarning("Shocker control was denied");
                continue;
            }

            if (!finalMessages.ContainsKey(shockerInfo.Device))
                finalMessages[shockerInfo.Device] = new List<ControlMessage.ShockerControlInfo>();
            var deviceGroup = finalMessages[shockerInfo.Device];

            var deviceEntry = new ControlMessage.ShockerControlInfo
            {
                Id = shockerInfo.Id,
                RfId = shockerInfo.RfId,
                Duration = Math.Clamp(shock.Duration, 300, 30000),
                Intensity = Math.Clamp(shock.Intensity, (byte)1, (byte)100),
                Type = shock.Type
            };
            deviceGroup.Add(deviceEntry);

            db.ShockerControlLogs.Add(new ShockerControlLog
            {
                Id = Guid.NewGuid(),
                ShockerId = shockerInfo.Id,
                ControlledBy = _currentUser.DbUser.Id,
                CreatedOn = curTime,
                Intensity = deviceEntry.Intensity,
                Duration = deviceEntry.Duration,
                Type = deviceEntry.Type
            });
        }

        var redisTask = PubSubManager.SendControlMessage(new ControlMessage
        {
            Shocker = _currentUser.DbUser.Id,
            ControlMessages = finalMessages
        });

        await Task.WhenAll(redisTask, db.SaveChangesAsync());
    }

    protected override async Task SendInitialData()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShockLinkContext>();
        var sharedDevices = await db.Devices
            .Where(x => x.Shockers.Any(y => y.ShockerShares.Any(z => z.SharedWith == _currentUser.DbUser.Id)))
            .Select(x => x.Id.ToString()).ToListAsync();

        var own = _devicesOnline.Where(x => x.Owner == _currentUser.DbUser.Id).ToListAsync();
        var shared = _devicesOnline.FindByIdsAsync(sharedDevices);
        await Task.WhenAll(own, shared);

        var final = new List<DeviceOnlineState>();
        final.AddRange(own.Result.Select(x =>
            new DeviceOnlineState
            {
                Device = x.Id,
                Online = true
            }));
        final.AddRange(shared.Result.Values.Where(x => x != null).Select(x =>
            new DeviceOnlineState
            {
                Device = x!.Id,
                Online = true
            }));
        await QueueMessage(new BaseResponse<ResponseType>
        {
            ResponseType = ResponseType.DeviceOnlineState,
            Data = final
        });
    }
}