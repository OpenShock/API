using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Extensions;

namespace OpenShock.API.Controller.Shares;

public sealed partial class SharesController
{
    [HttpGet]
    [ApiVersion("2")]
    public IAsyncEnumerable<V2UserSharesListItem> GetSharesByUsers()
    {
        return _db.ShockerShares
            .Where(x => x.Shocker.Device.OwnerId == CurrentUser.Id)
            .AsNoTracking()
            .GroupBy(x => x.SharedWithUserId)
            .Select(g => new V2UserSharesListItem
            {
                Id = g.Key,
                Image = g.First().SharedWithUser.GetImageUrl(),
                Name = g.First().SharedWithUser.Name,
                Shares = g.Select(y => new UserShareInfo
                    {
                        Id = y.Shocker.Id,
                        Name = y.Shocker.Name,
                        CreatedOn = y.CreatedAt,
                        Permissions = new ShockerPermissions
                        {
                            Vibrate = y.AllowVibrate,
                            Sound = y.AllowSound,
                            Shock = y.AllowShock,
                            Live = y.AllowLiveControl
                        },
                        Limits = new ShockerLimits
                        {
                            Intensity = y.MaxIntensity,
                            Duration = y.MaxDuration
                        },
                        Paused = y.IsPaused
                    })
                    .ToArray()
            })
            .AsAsyncEnumerable();
    }
}