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
                x.Shocker.Id,
                x.Shocker.Name,
                x.PermVibrate,
                x.PermSound,
                x.PermShock
            }).ToListAsync();

        var shared = new Dictionary<Guid, OwnerShockerResponse>();
        foreach (var shocker in sharedShockersRaw)
        {
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
            
            var sharedDevice = sharedUser.Devices.Single(x => x.Id == shocker.DeviceId);
            sharedDevice.Shockers.Add(new OwnerShockerResponse.SharedDevice.SharedShocker
            {
                Id = shocker.Id,
                Name = shocker.Name,
                PermShock = shocker.PermShock,
                PermSound = shocker.PermVibrate,
                PermVibrate = shocker.PermVibrate
            });
        }

        return new BaseResponse<IEnumerable<OwnerShockerResponse>>
        {
            Data = shared.Values
        };
    }
}