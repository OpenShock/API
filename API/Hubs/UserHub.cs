using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.DeviceControl;
using OpenShock.API.Models.WebSocket;
using OpenShock.API.Realtime;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.ShockLinkDb;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.Utils;
using Redis.OM;
using Redis.OM.Contracts;

namespace OpenShock.API.Hubs;

[Authorize(AuthenticationSchemes = ShockLinkAuthSchemas.SessionTokenCombo)]
public class UserHub : Hub<IUserHub>
{
    private readonly ILogger<UserHub> _logger;
    private readonly ShockLinkContext _db;
    private readonly IRedisConnectionProvider _provider;

    public UserHub(ILogger<UserHub> logger, ShockLinkContext db, IRedisConnectionProvider provider)
    {
        _logger = logger;
        _db = db;
        _provider = provider;
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.Welcome(Context.ConnectionId);
        var devicesOnline = _provider.RedisCollection<DeviceOnline>(false);
        var sharedDevices = await _db.Devices
            .Where(x => x.Shockers.Any(y => y.ShockerShares.Any(z => z.SharedWith == UserId)))
            .Select(x => x.Id.ToString()).ToArrayAsync();

        var own = devicesOnline.Where(x => x.Owner == UserId).ToArrayAsync();
        var shared = devicesOnline.FindByIdsAsync(sharedDevices);
        await Task.WhenAll(own, shared);

        var final = new List<DeviceOnlineState>();
        final.AddRange(own.Result.Select(x =>
            new DeviceOnlineState
            {
                Device = x.Id,
                Online = true,
                FirmwareVersion = x.FirmwareVersion
            }));
        final.AddRange(shared.Result.Values.Where(x => x != null).Select(x =>
            new DeviceOnlineState
            {
                Device = x!.Id,
                Online = true,
                FirmwareVersion = x.FirmwareVersion
            }));
        await Clients.Caller.DeviceStatus(final);
    }

    public Task Control(IEnumerable<Common.Models.WebSocket.User.Control> shocks)
    {
        return ControlV2(shocks, null);
    }

    public async Task ControlV2(IEnumerable<Common.Models.WebSocket.User.Control> shocks, string? customName)
    {
        var additionalItems = new Dictionary<string, object>();
        var apiTokenId = Context.User?.FindFirst(ControlLogAdditionalItem.ApiTokenId);
        if (apiTokenId != null) additionalItems[ControlLogAdditionalItem.ApiTokenId] = apiTokenId.Value;

        var sender = await _db.Users.Where(x => x.Id == UserId).Select(x => new ControlLogSender
        {
            Id = x.Id,
            Name = x.Name,
            Image = GravatarUtils.GetImageUrl(x.Email),
            ConnectionId = Context.ConnectionId,
            AdditionalItems = additionalItems,
            CustomName = customName
        }).SingleAsync();

        await ControlLogic.ControlByUser(shocks, _db, sender, Clients);
    }

    public async Task CaptivePortal(Guid deviceId, bool enabled)
    {
        var devices = await _db.Devices.Where(x => x.Owner == UserId)
            .AnyAsync(x => x.Id == deviceId);
        if (!devices) return;

        await PubSubManager.SendCaptiveControlMessage(new CaptiveMessage
        {
            DeviceId = deviceId,
            Enabled = enabled
        });
    }

    private Task<User> GetUser() => GetUser(UserId, _db);

    private Guid UserId => _userId ??= Guid.Parse(Context.UserIdentifier!);
    private Guid? _userId;
    private static Task<User> GetUser(Guid userId, ShockLinkContext db) => db.Users.SingleAsync(x => x.Id == userId);
}