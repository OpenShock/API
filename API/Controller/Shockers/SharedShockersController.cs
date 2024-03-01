using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Shockers;

public partial class ShockerController
{
    /// <summary>
    /// List all shockers shared with the authenticated user.
    /// </summary>
    /// <response code="200">The shockers were successfully retrieved.</response>
    [HttpGet("shared")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<IEnumerable<OwnerShockerResponse>>> ListSharedShockers()
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
                    Permissions = new ShockerPermissions
                    {
                        Shock = x.PermShock,
                        Sound = x.PermSound,
                        Vibrate = x.PermVibrate,
                        Live = x.PermLive
                    },
                    Limits = new ShockerLimits
                    {
                        Duration = x.LimitDuration,
                        Intensity = x.LimitIntensity
                    }
                }
            }).ToListAsync();

        Dictionary<Guid, OwnerShockerResponse> shared = [];
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