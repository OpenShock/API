using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers/shared")]
public class SharedShockersController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;

    public SharedShockersController(ShockLinkContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<BaseResponse<IEnumerable<OwnerShockerResponse>>> GetSharedShockers()
    {
        var sharedShockersRaw = await _db.ShockerShares.Where(x => x.SharedWith == CurrentUser.DbUser.Id).Select(x =>
            new
            {
                OwnerId = x.Shocker.DeviceNavigation.OwnerNavigation.Id,
                OwnerName = x.Shocker.DeviceNavigation.OwnerNavigation.Name,
                DeviceId = x.Shocker.DeviceNavigation.Id,
                DeviceName = x.Shocker.DeviceNavigation.Name,
                Shocker = new OwnerShockerResponse.SharedDevice.SharedShocker
                {
                    Id = x.Shocker.Id,
                    Name = x.Shocker.Name,
                    IsPaused = x.Shocker.Paused,
                    PermShock = x.PermShock!.Value,
                    PermSound = x.PermVibrate!.Value,
                    PermVibrate = x.PermVibrate!.Value,
                    LimitDuration = x.LimitDuration,
                    LimitIntensity = x.LimitIntensity
                }
            }).ToListAsync();

        var shared = new Dictionary<Guid, OwnerShockerResponse>();
        foreach (var shocker in sharedShockersRaw)
        {
            // No I dont want unnecessary alloc
            // ReSharper disable once CanSimplifyDictionaryLookupWithTryAdd
            if (!shared.ContainsKey(shocker.OwnerId))
                shared[shocker.OwnerId] = new OwnerShockerResponse
                {
                    Id = shocker.OwnerId,
                    Name = shocker.OwnerName
                };

            var sharedUser = shared[shocker.OwnerId];

            if (sharedUser.Devices.All(x => x.Id != shocker.DeviceId))
                sharedUser.Devices.Add(new OwnerShockerResponse.SharedDevice
                {
                    Id = shocker.DeviceId,
                    Name = shocker.DeviceName
                });
            
            sharedUser.Devices.Single(x => x.Id == shocker.DeviceId).Shockers.Add(shocker.Shocker);
        }

        return new BaseResponse<IEnumerable<OwnerShockerResponse>>
        {
            Data = shared.Values
        };
    }
}