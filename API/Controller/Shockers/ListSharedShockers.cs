using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Shockers;

file record OwnerGroupKey(Guid OwnerId, string OwnerName, string OwnerEmail)
{
    public override int GetHashCode() => OwnerId.GetHashCode();
}

file record DeviceGroupKey(Guid DeviceId, string DeviceName)
{
    public override int GetHashCode() => DeviceId.GetHashCode();
}

public sealed partial class ShockerController
{
    /// <summary>
    /// List all shockers shared with the authenticated user.
    /// </summary>
    /// <response code="200">The shockers were successfully retrieved.</response>
    [HttpGet("shared")]
    [MapToApiVersion("1")]
    public async Task<LegacyDataResponse<IEnumerable<OwnerShockerResponse>>> ListSharedShockers()
    {
        var sharedShockersData = await _db.UserShares
            .AsNoTracking()
            .Where(x => x.SharedWithUserId == CurrentUser.Id)
            .Select(x => new
            {
                OwnerId = x.Shocker.Device.Owner.Id,
                OwnerName = x.Shocker.Device.Owner.Name,
                OwnerEmail = x.Shocker.Device.Owner.Email,
                DeviceId = x.Shocker.Device.Id,
                DeviceName = x.Shocker.Device.Name,
                Shocker = new OwnerShockerResponse.SharedDevice.SharedShocker
                {
                    Id = x.Shocker.Id,
                    Name = x.Shocker.Name,
                    IsPaused = x.Shocker.IsPaused,
                    Permissions = new ShockerPermissions
                    {
                        Vibrate = x.AllowVibrate,
                        Sound = x.AllowSound,
                        Shock = x.AllowShock,
                        Live = x.AllowLiveControl
                    },
                    Limits = new ShockerLimits
                    {
                        Intensity = x.MaxIntensity,
                        Duration = x.MaxDuration
                    }
                }
            })
            .ToArrayAsync();

        var sharesResponse = sharedShockersData
            .GroupBy(x => new OwnerGroupKey(x.OwnerId, x.OwnerName, x.OwnerEmail))
            .Select(ownerGroup => new OwnerShockerResponse
            {
                Id = ownerGroup.Key.OwnerId,
                Name = ownerGroup.Key.OwnerName,
                Image = GravatarUtils.GetUserImageUrl(ownerGroup.Key.OwnerEmail),
                Devices = ownerGroup
                    .GroupBy(x => new DeviceGroupKey(x.DeviceId, x.DeviceName))
                    .Select(deviceGroup => new OwnerShockerResponse.SharedDevice
                    {
                        Id = deviceGroup.Key.DeviceId,
                        Name = deviceGroup.Key.DeviceName,
                        Shockers = deviceGroup
                            .Select(x => x.Shocker)
                            .ToArray(),
                    })
                    .ToArray()
            });

        return new(sharesResponse);
    }
}