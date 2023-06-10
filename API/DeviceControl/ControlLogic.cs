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
    public static async Task<OneOf<Success>> Control(IEnumerable<Common.Models.WebSocket.User.Control> shocks, ShockLinkContext db, Guid userId, IHubClients<IUserHub> hubClients)
    {
        var finalMessages = new Dictionary<Guid, IList<ControlMessage.ShockerControlInfo>>();
        
        var ownShockers = await db.Shockers.Where(x => x.DeviceNavigation.Owner == userId).Select(x =>
            new
            {
                x.Id,
                x.Name,
                x.RfId,
                x.Device,
                x.DeviceNavigation.Owner,
                x.Model,
                x.Paused
            }).ToListAsync();

        var sharedShockers = await db.ShockerShares.Where(x => x.SharedWith == userId).Select(x => new
        {
            x.Shocker.Id,
            x.Shocker.Name,
            x.Shocker.RfId,
            x.Shocker.Device,
            x.Shocker.DeviceNavigation.Owner,
            x.Shocker.Model,
            x.Shocker.Paused
        }).ToListAsync();

        ownShockers.AddRange(sharedShockers);

        var curTime = DateTime.UtcNow;
        var distinctShocks = shocks.DistinctBy(x => x.Id).ToArray();
        var logs = new Dictionary<Guid, List<ControlLog>>();
        
        foreach (var shock in distinctShocks)
        {
            var shockerInfo = ownShockers.FirstOrDefault(x => x.Id == shock.Id);
            if (shockerInfo == null)
            {
                // TODO: Return denied
                continue;
            }

            if (shockerInfo.Paused)
            {
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
                Type = shock.Type,
                Model = shockerInfo.Model
            };
            deviceGroup.Add(deviceEntry);

            db.ShockerControlLogs.Add(new ShockerControlLog
            {
                Id = Guid.NewGuid(),
                ShockerId = shockerInfo.Id,
                ControlledBy = userId,
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
                Intensity = deviceEntry.Intensity
            });
            
        }

        var redisTask = PubSubManager.SendControlMessage(new ControlMessage
        {
            Shocker = userId,
            ControlMessages = finalMessages
        });

        await Task.WhenAll(redisTask, db.SaveChangesAsync());

        var sender = await db.Users.Where(x => x.Id == userId).Select(x => new GenericIni
        {
            Id = x.Id,
            Name = x.Name,
            Image = ImagesApi.GetImageRoot(x.Image)
        }).SingleAsync();
        
        var logSends = logs.Select(x => hubClients.User(x.Key.ToString()).Log(sender, x.Value));
        await Task.WhenAll(logSends);

        return new OneOf<Success>();
    }
}