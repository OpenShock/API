using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Constants;
using OpenShock.Common.DeviceControl;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket.User;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Services;

public sealed class ControlSender : IControlSender
{
    private readonly OpenShockContext _db;
    private readonly IRedisPubService _publisher;

    public ControlSender(OpenShockContext db, IRedisPubService publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlByUser(IReadOnlyList<Control> controls,ControlLogSender sender, IHubClients<IUserHub> hubClients)
    {
        var queryOwn = _db.Shockers
            .AsNoTracking()
            .Where(x => x.Device.OwnerId == sender.Id)
            .Select(x => new ControlShockerObj
            {
                ShockerId = x.Id,
                ShockerName = x.Name,
                ShockerRfId = x.RfId,
                DeviceId = x.DeviceId,
                ShockerModel = x.Model,
                OwnerId = x.Device.OwnerId,
                Paused = x.IsPaused,
                PermsAndLimits = null
            });

        var queryShared = _db.UserShares
            .AsNoTracking()
            .Where(x => x.SharedWithUserId == sender.Id)
            .Select(x => new ControlShockerObj
            {
                ShockerId = x.Shocker.Id,
                ShockerName = x.Shocker.Name,
                ShockerRfId = x.Shocker.RfId,
                DeviceId = x.Shocker.DeviceId,
                ShockerModel = x.Shocker.Model,
                OwnerId = x.Shocker.Device.OwnerId,
                Paused = x.Shocker.IsPaused || x.IsPaused,
                PermsAndLimits = new SharePermsAndLimits
                {
                    Sound = x.AllowSound,
                    Vibrate = x.AllowVibrate,
                    Shock = x.AllowShock,
                    Duration = x.MaxDuration,
                    Intensity = x.MaxIntensity,
                    Live = x.AllowLiveControl
                }
            });

        var shockers = await queryOwn.Concat(queryShared).ToArrayAsync();

        return await ControlInternal(controls, sender, hubClients, shockers);
    }

    public async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlPublicShare(IReadOnlyList<Control> controls, ControlLogSender sender, IHubClients<IUserHub> hubClients, Guid publicShareId)
    {
        var publicShareShockers = await _db.PublicShareShockerMappings
            .AsNoTracking()
            .Where(x => x.PublicShareId == publicShareId && (x.PublicShare.ExpiresAt > DateTime.UtcNow || x.PublicShare.ExpiresAt == null))
            .Select(x => new ControlShockerObj
            {
                ShockerId = x.Shocker.Id,
                ShockerName = x.Shocker.Name,
                ShockerRfId = x.Shocker.RfId,
                DeviceId = x.Shocker.DeviceId,
                ShockerModel = x.Shocker.Model,
                OwnerId = x.Shocker.Device.OwnerId,
                Paused = x.Shocker.IsPaused || x.IsPaused,
                PermsAndLimits = new SharePermsAndLimits
                {
                    Sound = x.AllowSound,
                    Vibrate = x.AllowVibrate,
                    Shock = x.AllowShock,
                    Duration = x.MaxDuration,
                    Intensity = x.MaxIntensity,
                    Live = x.AllowLiveControl
                }
            })
            .ToArrayAsync();
        
        return await ControlInternal(controls, sender, hubClients, publicShareShockers);
    }
    
    private static void Clamp(Control control, SharePermsAndLimits? limits)
    {
        var durationMax = limits?.Duration ?? HardLimits.MaxControlDuration;
        var intensityMax = limits?.Intensity ?? HardLimits.MaxControlIntensity;

        control.Intensity = Math.Clamp(control.Intensity, HardLimits.MinControlIntensity, intensityMax);
        control.Duration = Math.Clamp(control.Duration, HardLimits.MinControlDuration, durationMax);
    }

    private async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlInternal(IReadOnlyList<Control> controls, ControlLogSender sender, IHubClients<IUserHub> hubClients, ControlShockerObj[] allowedShockers)
    {
        var shockersById = allowedShockers.ToDictionary(s => s.ShockerId, s => s);

        var now = DateTime.UtcNow;
        
        var messagesByDevice = new Dictionary<Guid, List<ShockerControlCommand>>();
        var logsByOwner = new Dictionary<Guid, List<ControlLog>>();

        foreach (var control in controls.DistinctBy(x => x.Id))
        {
            if (!shockersById.TryGetValue(control.Id, out var shocker))
                return new ShockerNotFoundOrNoAccess(control.Id);

            if (shocker.Paused)
                return new ShockerPaused(control.Id);

            if (!PermissionUtils.IsAllowed(control.Type, false, shocker.PermsAndLimits))
                return new ShockerNoPermission(control.Id);

            Clamp(control, shocker.PermsAndLimits);

            messagesByDevice.AppendValue(shocker.DeviceId, new ShockerControlCommand
            {
                ShockerId = shocker.ShockerId,
                RfId = shocker.ShockerRfId,
                Duration = control.Duration,
                Intensity = control.Intensity,
                Type = control.Type,
                Model = shocker.ShockerModel,
                Exclusive = control.Exclusive
            });
            logsByOwner.AppendValue(shocker.OwnerId, new ControlLog
            {
                Shocker = new BasicShockerInfo
                {
                    Id = shocker.ShockerId,
                    Name = shocker.ShockerName
                },
                Type = control.Type,
                Intensity = control.Intensity,
                Duration = control.Duration,
                ExecutedAt = now
            });

            _db.ShockerControlLogs.Add(new ShockerControlLog
            {
                Id = Guid.CreateVersion7(),
                ShockerId = shocker.ShockerId,
                ControlledByUserId = sender.Id == Guid.Empty ? null : sender.Id,
                Intensity = control.Intensity,
                Duration = control.Duration,
                Type = control.Type,
                CustomName = sender.CustomName,
                CreatedAt = now
            });
        }

        // Save all db changes before continuing
        await _db.SaveChangesAsync();

        // Then send all network events
        await Task.WhenAll([
            ..messagesByDevice.Select(kvp => _publisher.SendDeviceControl(kvp.Key, kvp.Value)),
            ..logsByOwner.Select(x => hubClients.User(x.Key.ToString()).Log(sender, x.Value))
            ]);

        return new Success();
    }
}