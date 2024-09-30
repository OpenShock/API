using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.API.DeviceControl;
using OpenShock.Common;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket.User;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Models;
using OpenShock.ServicesCommon.Services.RedisPubSub;

namespace OpenShock.ServicesCommon.DeviceControl;

public static class ControlLogic
{
    public static async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlByUser(IEnumerable<Control> shocks, OpenShockContext db, ControlLogSender sender,
        IHubClients<IUserHub> hubClients, IRedisPubService redisPubService)
    {
        var ownShockers = await db.Shockers.Where(x => x.DeviceNavigation.Owner == sender.Id).Select(x =>
            new ControlShockerObj
            {
                Id = x.Id,
                Name = x.Name,
                RfId = x.RfId,
                Device = x.Device,
                Model = x.Model,
                Owner = x.DeviceNavigation.Owner,
                Paused = x.Paused,
                PermsAndLimits = null
            }).ToListAsync();
        
        var sharedShockers = await db.ShockerShares.Where(x => x.SharedWith == sender.Id).Select(x =>
            new ControlShockerObj
            {
                Id = x.Shocker.Id,
                Name = x.Shocker.Name,
                RfId = x.Shocker.RfId,
                Device = x.Shocker.Device,
                Model = x.Shocker.Model,
                Owner = x.Shocker.DeviceNavigation.Owner,
                Paused = x.Shocker.Paused || x.Paused,
                PermsAndLimits = new SharePermsAndLimits
                {
                    Shock = x.PermShock,
                    Vibrate = x.PermVibrate,
                    Sound = x.PermSound,
                    Duration = x.LimitDuration,
                    Intensity = x.LimitIntensity
                }
            }).ToListAsync();

        ownShockers.AddRange(sharedShockers);

        return await ControlInternal(shocks, db, sender, hubClients, ownShockers, redisPubService);
    }

    public static async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlShareLink(IEnumerable<Control> shocks, OpenShockContext db,
        ControlLogSender sender,
        IHubClients<IUserHub> hubClients, Guid shareLinkId, IRedisPubService redisPubService)
    {
        var shareLinkShockers = await db.ShockerSharesLinksShockers.Where(x => x.ShareLinkId == shareLinkId && (x.ShareLink.ExpiresOn > DateTime.UtcNow || x.ShareLink.ExpiresOn == null))
            .Select(x => new ControlShockerObj
        {
            Id = x.Shocker.Id,
            Name = x.Shocker.Name,
            RfId = x.Shocker.RfId,
            Device = x.Shocker.Device,
            Model = x.Shocker.Model,
            Owner = x.Shocker.DeviceNavigation.Owner,
            Paused = x.Shocker.Paused || x.Paused,
            PermsAndLimits = new SharePermsAndLimits
            {
                Shock = x.PermShock,
                Vibrate = x.PermVibrate,
                Sound = x.PermSound,
                Duration = x.LimitDuration,
                Intensity = x.LimitIntensity
            }
        }).ToListAsync();
        return await ControlInternal(shocks, db, sender, hubClients, shareLinkShockers, redisPubService);
    }
    
    private static async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlInternal(IEnumerable<Control> shocks, OpenShockContext db, ControlLogSender sender,
        IHubClients<IUserHub> hubClients, IReadOnlyCollection<ControlShockerObj> allowedShockers, IRedisPubService redisPubService)
    {
        var finalMessages = new Dictionary<Guid, IList<ControlMessage.ShockerControlInfo>>();
        var curTime = DateTime.UtcNow;
        var distinctShocks = shocks.DistinctBy(x => x.Id);
        var logs = new Dictionary<Guid, List<ControlLog>>();

        foreach (var shock in distinctShocks)
        {
            var shockerInfo = allowedShockers.FirstOrDefault(x => x.Id == shock.Id);
            
            if (shockerInfo == null) return new ShockerNotFoundOrNoAccess(shock.Id);
            
            if (shockerInfo.Paused) return new ShockerPaused(shock.Id);

            if (!IsAllowed(shock.Type, shockerInfo.PermsAndLimits)) return new ShockerNoPermission(shock.Id);
            var durationMax = shockerInfo.PermsAndLimits?.Duration ?? Constants.MaxControlDuration;
            var intensityMax = shockerInfo.PermsAndLimits?.Intensity ?? Constants.MaxControlIntensity;

            if (!finalMessages.TryGetValue(shockerInfo.Device, out var deviceGroup))
            {
                deviceGroup = [];
                finalMessages[shockerInfo.Device] = deviceGroup;
            }

            var intensity = Math.Clamp(shock.Intensity, Constants.MinControlIntensity, intensityMax);
            var duration = Math.Clamp(shock.Duration, Constants.MinControlDuration, durationMax);

            deviceGroup.Add(new ControlMessage.ShockerControlInfo
            {
                Id = shockerInfo.Id,
                RfId = shockerInfo.RfId,
                Duration = duration,
                Intensity = intensity,
                Type = shock.Type,
                Model = shockerInfo.Model,
                Exclusive = shock.Exclusive
            });

            db.ShockerControlLogs.Add(new ShockerControlLog
            {
                Id = Guid.NewGuid(),
                ShockerId = shockerInfo.Id,
                ControlledBy = sender.Id == Guid.Empty ? null : sender.Id,
                CreatedOn = curTime,
                Intensity = intensity,
                Duration = duration,
                Type = shock.Type,
                CustomName = sender.CustomName
            });

            if (!logs.TryGetValue(shockerInfo.Owner, out var ownerLog))
            {
                ownerLog = [];
                logs[shockerInfo.Owner] = ownerLog;
            }

            ownerLog.Add(new ControlLog
            {
                Shocker = new GenericIn
                {
                    Id = shockerInfo.Id,
                    Name = shockerInfo.Name
                },
                Type = shock.Type,
                Duration = duration,
                Intensity = intensity,
                ExecutedAt = curTime
            });
        }

        var redisTask =  redisPubService.SendDeviceControl(sender.Id, finalMessages);
        var logSends = logs.Select(x => hubClients.User(x.Key.ToString()).Log(sender, x.Value));

        await Task.WhenAll([
            redisTask,
            db.SaveChangesAsync(),
            ..logSends
            ]);

        return new Success();
    }

    private static bool IsAllowed(ControlType type, SharePermsAndLimits? perms)
    {
        if (perms == null) return true;
        return type switch
        {
            ControlType.Shock => perms.Shock,
            ControlType.Vibrate => perms.Vibrate,
            ControlType.Sound => perms.Sound,
            ControlType.Stop => perms.Shock || perms.Vibrate || perms.Sound,
            _ => false
        };
    }
}

public readonly record struct ShockerNotFoundOrNoAccess(Guid Value);

public readonly record struct ShockerPaused(Guid Value);

public readonly record struct ShockerNoPermission(Guid Value);