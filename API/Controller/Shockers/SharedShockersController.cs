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


            shared[shocker.OwnerId].Shockers.Add(new OwnerShockerResponse.SharedShocker()
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