using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using ShockLink.API.Realtime;
using ShockLink.Common.Redis.PubSub;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Control;

public static class ControlLogic
{
    public static async Task<OneOf<Success>> Control(IEnumerable<Common.Models.WebSocket.User.Control> shocks, ShockLinkContext db, Guid userId)
    {
        var finalMessages = new Dictionary<Guid, IList<ControlMessage.ShockerControlInfo>>();
        
        var ownShockers = await db.Shockers.Where(x => x.DeviceNavigation.Owner == userId).Select(x =>
            new
            {
                x.Id,
                x.RfId,
                x.Device
            }).ToListAsync();

        var sharedShockers = await db.ShockerShares.Where(x => x.SharedWith == userId).Select(x => new
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
                // TODO: Return denied
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
                ControlledBy = userId,
                CreatedOn = curTime,
                Intensity = deviceEntry.Intensity,
                Duration = deviceEntry.Duration,
                Type = deviceEntry.Type
            });
        }

        var redisTask = PubSubManager.SendControlMessage(new ControlMessage
        {
            Shocker = userId,
            ControlMessages = finalMessages
        });

        await Task.WhenAll(redisTask, db.SaveChangesAsync());

        return new OneOf<Success>();
    }
}