using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using ShockLink.API.Hubs;
using ShockLink.API.Realtime;
using ShockLink.API.Utils;
using ShockLink.Common.Models;
using ShockLink.Common.Models.WebSocket.User;
using ShockLink.Common.Redis.PubSub;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.DeviceControl;

public static class ControlLogic
{

    public static async Task<OneOf<Success>> ControlByUser(IEnumerable<Control> shocks, ShockLinkContext db, ControlLogSender sender,
        IHubClients<IUserHub> hubClients)
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
                Paused = x.Shocker.Paused,
                PermsAndLimits = new ControlShockerObj.SharePermsAndLimits()
                {
                    Shock = x.PermShock!.Value,
                    Vibrate = x.PermVibrate!.Value,
                    Sound = x.PermSound!.Value,
                    Duration = x.LimitDuration,
                    Intensity = x.LimitIntensity
                }
            }).ToListAsync();

        ownShockers.AddRange(sharedShockers);

        return await ControlInternal(shocks, db, sender, hubClients, ownShockers);
    }

    public static async Task<OneOf<Success>> ControlShareLink(IEnumerable<Control> shocks, ShockLinkContext db,
        ControlLogSender sender,
        IHubClients<IUserHub> hubClients, Guid shareLinkId)
    {
        var shareLinkShockers = await db.ShockerSharesLinksShockers.Where(x => x.ShareLinkId == shareLinkId)
            .Select(x => new ControlShockerObj
        {
            Id = x.Shocker.Id,
            Name = x.Shocker.Name,
            RfId = x.Shocker.RfId,
            Device = x.Shocker.Device,
            Model = x.Shocker.Model,
            Owner = x.Shocker.DeviceNavigation.Owner,
            Paused = x.Shocker.Paused,
            PermsAndLimits = new ControlShockerObj.SharePermsAndLimits()
            {
                Shock = x.PermShock,
                Vibrate = x.PermVibrate,
                Sound = x.PermSound,
                Duration = x.LimitDuration,
                Intensity = x.LimitIntensity
            }
        }).ToListAsync();
        return await ControlInternal(shocks, db, sender, hubClients, shareLinkShockers);
    }
    
    private static async Task<OneOf<Success>> ControlInternal(IEnumerable<Control> shocks, ShockLinkContext db, ControlLogSender sender,
        IHubClients<IUserHub> hubClients, IReadOnlyCollection<ControlShockerObj> allowedShockers)
    {
        var finalMessages = new Dictionary<Guid, IList<ControlMessage.ShockerControlInfo>>();
        var curTime = DateTime.UtcNow;
        var distinctShocks = shocks.DistinctBy(x => x.Id);
        var logs = new Dictionary<Guid, List<ControlLog>>();

        foreach (var shock in distinctShocks)
        {
            var shockerInfo = allowedShockers.FirstOrDefault(x => x.Id == shock.Id);
            if (shockerInfo == null)
            {
                // TODO: Return denied
                continue;
            }

            if (shockerInfo.Paused) continue;

            if (!IsAllowed(shock.Type, shockerInfo.PermsAndLimits)) continue;
            var durationMax = shockerInfo.PermsAndLimits?.Duration ?? 30000;
            var intensityMax = shockerInfo.PermsAndLimits?.Intensity ?? 100;

            if (!finalMessages.ContainsKey(shockerInfo.Device))
                finalMessages[shockerInfo.Device] = new List<ControlMessage.ShockerControlInfo>();
            var deviceGroup = finalMessages[shockerInfo.Device];

            var deviceEntry = new ControlMessage.ShockerControlInfo
            {
                Id = shockerInfo.Id,
                RfId = shockerInfo.RfId,
                Duration = Math.Clamp(shock.Duration, 300, durationMax),
                Intensity = Math.Clamp(shock.Intensity, (byte)1, intensityMax),
                Type = shock.Type,
                Model = shockerInfo.Model
            };
            deviceGroup.Add(deviceEntry);

            db.ShockerControlLogs.Add(new ShockerControlLog
            {
                Id = Guid.NewGuid(),
                ShockerId = shockerInfo.Id,
                ControlledBy = sender.Id == Guid.Empty ? null : sender.Id,
                CreatedOn = curTime,
                Intensity = deviceEntry.Intensity,
                Duration = deviceEntry.Duration,
                Type = deviceEntry.Type
            });

            if (!logs.ContainsKey(shockerInfo.Owner)) logs[shockerInfo.Owner] = new List<ControlLog>();

            logs[shockerInfo.Owner].Add(new ControlLog
            {
                Shocker = new GenericIn
                {
                    Id = shockerInfo.Id,
                    Name = shockerInfo.Name
                },
                Type = deviceEntry.Type,
                Duration = deviceEntry.Duration,
                Intensity = deviceEntry.Intensity,
                ExecutedAt = curTime
            });
        }

        var redisTask = PubSubManager.SendControlMessage(new ControlMessage
        {
            Shocker = sender.Id,
            ControlMessages = finalMessages
        });
        var logSends = logs.Select(x => hubClients.User(x.Key.ToString()).Log(sender, x.Value));

        var listOfTasks = new List<Task>
        {
            redisTask,
            db.SaveChangesAsync()
        };
        listOfTasks.AddRange(logSends);

        await Task.WhenAll(listOfTasks);

        return new OneOf<Success>();
    }

    private static bool IsAllowed(ControlType type, ControlShockerObj.SharePermsAndLimits? perms)
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