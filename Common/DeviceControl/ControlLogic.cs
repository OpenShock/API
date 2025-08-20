using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Constants;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket.User;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Utils;

namespace OpenShock.Common.DeviceControl;

public static class ControlLogic
{
    public static async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlByUser(IReadOnlyList<Control> shocks, OpenShockContext db, ControlLogSender sender,
        IHubClients<IUserHub> hubClients, IRedisPubService redisPubService)
    {
        var queryOwn = db.Shockers
            .AsNoTracking()
            .Where(x => x.Device.OwnerId == sender.UserId || x.UserShares.Any(u => u.SharedWithUserId == sender.UserId))
            .Select(x => new ControlShockerObj
            {
                Id = x.Id,
                Name = x.Name,
                RfId = x.RfId,
                Device = x.DeviceId,
                Model = x.Model,
                OwnerId = x.Device.OwnerId,
                Paused = x.IsPaused,
                PermsAndLimits = null
            });

        var queryShared = db.UserShares
            .AsNoTracking()
            .Where(x => x.SharedWithUserId == sender.UserId)
            .Select(x => new ControlShockerObj
            {
                Id = x.Shocker.Id,
                Name = x.Shocker.Name,
                RfId = x.Shocker.RfId,
                Device = x.Shocker.DeviceId,
                Model = x.Shocker.Model,
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

        return await ControlInternal(shocks, db, sender, hubClients, shockers, redisPubService);
    }

    public static async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlPublicShare(IReadOnlyList<Control> shocks, OpenShockContext db,
        ControlLogSender sender,
        IHubClients<IUserHub> hubClients, Guid publicShareId, IRedisPubService redisPubService)
    {
        var publicShareShockers = await db.PublicShareShockerMappings.Where(x => x.PublicShareId == publicShareId && (x.PublicShare.ExpiresAt > DateTime.UtcNow || x.PublicShare.ExpiresAt == null))
            .Select(x => new ControlShockerObj
            {
                Id = x.Shocker.Id,
                Name = x.Shocker.Name,
                RfId = x.Shocker.RfId,
                Device = x.Shocker.DeviceId,
                Model = x.Shocker.Model,
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
            }).ToArrayAsync();
        
        return await ControlInternal(shocks, db, sender, hubClients, publicShareShockers, redisPubService);
    }
    
    private static async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlInternal(IReadOnlyList<Control> shocks, OpenShockContext db, ControlLogSender sender,
        IHubClients<IUserHub> hubClients, ControlShockerObj[] allowedShockers, IRedisPubService redisPubService)
    {
        var messages = new Dictionary<Guid, List<DeviceControlPayload.ShockerControlInfo>>();
        var logs = new Dictionary<Guid, List<ControlLog>>();
        var curTime = DateTime.UtcNow;
        var distinctShocks = shocks.DistinctBy(x => x.Id);

        foreach (var shock in distinctShocks)
        {
            var shockerInfo = allowedShockers.FirstOrDefault(x => x.Id == shock.Id);
            
            if (shockerInfo is null) return new ShockerNotFoundOrNoAccess(shock.Id);
            
            if (shockerInfo.Paused) return new ShockerPaused(shock.Id);

            if (!PermissionUtils.IsAllowed(shock.Type, false, shockerInfo.PermsAndLimits)) return new ShockerNoPermission(shock.Id);
            var durationMax = shockerInfo.PermsAndLimits?.Duration ?? HardLimits.MaxControlDuration;
            var intensityMax = shockerInfo.PermsAndLimits?.Intensity ?? HardLimits.MaxControlIntensity;

            var intensity = Math.Clamp(shock.Intensity, HardLimits.MinControlIntensity, intensityMax);
            var duration = Math.Clamp(shock.Duration, HardLimits.MinControlDuration, durationMax);

            messages.AppendValue(shockerInfo.Device, new DeviceControlPayload.ShockerControlInfo
            {
                RfId = shockerInfo.RfId,
                Duration = duration,
                Intensity = intensity,
                Type = shock.Type,
                Model = shockerInfo.Model,
                Exclusive = shock.Exclusive
            });
            logs.AppendValue(shockerInfo.OwnerId, new ControlLog
            {
                Shocker = new BasicShockerInfo
                {
                    Id = shockerInfo.Id,
                    Name = shockerInfo.Name
                },
                Type = shock.Type,
                Intensity = intensity,
                Duration = duration,
                ExecutedAt = curTime
            });

            db.ShockerControlLogs.Add(new ShockerControlLog
            {
                Id = Guid.CreateVersion7(),
                ShockerId = shockerInfo.Id,
                ControlledByUserId = sender.UserId == Guid.Empty ? null : sender.UserId,
                Intensity = intensity,
                Duration = duration,
                Type = shock.Type,
                CustomName = sender.CustomName,
                CreatedAt = curTime
            });
        }

        // Save all db cahnges before continuing
        await db.SaveChangesAsync();

        // Then send all network events
        await Task.WhenAll([
            ..messages.Select(kvp => redisPubService.SendDeviceControl(kvp.Key, kvp.Value)),
            ..logs.Select(x => hubClients.User(x.Key.ToString()).Log(sender, x.Value))
            ]);

        return new Success();
    }
}

public readonly record struct ShockerNotFoundOrNoAccess(Guid Value);

public readonly record struct ShockerPaused(Guid Value);

public readonly record struct ShockerNoPermission(Guid Value);