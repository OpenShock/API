﻿using Microsoft.AspNetCore.SignalR;
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

namespace OpenShock.Common.DeviceControl;

public static class ControlLogic
{
    public static async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlByUser(IReadOnlyList<Control> shocks, OpenShockContext db, ControlLogSender sender,
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
                Paused = x.IsPaused,
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
                Paused = x.Shocker.IsPaused || x.IsPaused,
                PermsAndLimits = new SharePermsAndLimits
                {
                    Sound = x.AllowSound,
                    Vibrate = x.AllowVibrate,
                    Shock = x.AllowShock,
                    Duration = x.MaxDuration,
                    Intensity = x.MaxIntensity
                }
            }).ToArrayAsync();

        ownShockers.AddRange(sharedShockers);

        return await ControlInternal(shocks, db, sender, hubClients, ownShockers, redisPubService);
    }

    public static async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlShareLink(IReadOnlyList<Control> shocks, OpenShockContext db,
        ControlLogSender sender,
        IHubClients<IUserHub> hubClients, Guid shareLinkId, IRedisPubService redisPubService)
    {
        var shareLinkShockers = await db.ShockerSharesLinksShockers.Where(x => x.ShareLinkId == shareLinkId && (x.ShareLink.ExpiresAt > DateTime.UtcNow || x.ShareLink.ExpiresAt == null))
            .Select(x => new ControlShockerObj
        {
            Id = x.Shocker.Id,
            Name = x.Shocker.Name,
            RfId = x.Shocker.RfId,
            Device = x.Shocker.Device,
            Model = x.Shocker.Model,
            Owner = x.Shocker.DeviceNavigation.Owner,
            Paused = x.Shocker.IsPaused || x.IsPaused,
            PermsAndLimits = new SharePermsAndLimits
            {
                Sound = x.AllowSound,
                Vibrate = x.AllowVibrate,
                Shock = x.AllowShock,
                Duration = x.MaxDuration,
                Intensity = x.MaxIntensity
            }
        }).ToArrayAsync();
        
        return await ControlInternal(shocks, db, sender, hubClients, shareLinkShockers, redisPubService);
    }
    
    private static async Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlInternal(IReadOnlyList<Control> shocks, OpenShockContext db, ControlLogSender sender,
        IHubClients<IUserHub> hubClients, IReadOnlyCollection<ControlShockerObj> allowedShockers, IRedisPubService redisPubService)
    {
        var finalMessages = new Dictionary<Guid, List<ControlMessage.ShockerControlInfo>>();
        var curTime = DateTime.UtcNow;
        var distinctShocks = shocks.DistinctBy(x => x.Id);
        var logs = new Dictionary<Guid, List<ControlLog>>();

        foreach (var shock in distinctShocks)
        {
            var shockerInfo = allowedShockers.FirstOrDefault(x => x.Id == shock.Id);
            
            if (shockerInfo == null) return new ShockerNotFoundOrNoAccess(shock.Id);
            
            if (shockerInfo.Paused) return new ShockerPaused(shock.Id);

            if (!IsAllowed(shock.Type, shockerInfo.PermsAndLimits)) return new ShockerNoPermission(shock.Id);
            var durationMax = shockerInfo.PermsAndLimits?.Duration ?? HardLimits.MaxControlDuration;
            var intensityMax = shockerInfo.PermsAndLimits?.Intensity ?? HardLimits.MaxControlIntensity;

            var deviceGroup = finalMessages.GetValueOrAddDefault(shockerInfo.Device, []);

            var intensity = Math.Clamp(shock.Intensity, HardLimits.MinControlIntensity, intensityMax);
            var duration = Math.Clamp(shock.Duration, HardLimits.MinControlDuration, durationMax);

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
                Id = Guid.CreateVersion7(),
                ShockerId = shockerInfo.Id,
                ControlledBy = sender.Id == Guid.Empty ? null : sender.Id,
                CreatedAt = curTime,
                Intensity = intensity,
                Duration = duration,
                Type = shock.Type,
                CustomName = sender.CustomName
            });

            var ownerLog = logs.GetValueOrAddDefault(shockerInfo.Owner, []);

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

        var redisTask = redisPubService.SendDeviceControl(sender.Id, finalMessages
            .ToDictionary(kvp => kvp.Key, IReadOnlyList<ControlMessage.ShockerControlInfo> (kvp) => kvp.Value));
        
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