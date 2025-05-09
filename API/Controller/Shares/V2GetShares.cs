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
            .Where(x => x.Shocker.DeviceNavigation.Owner == CurrentUser.Id)
            .AsNoTracking()
            .GroupBy(x => x.SharedWith)
            .Select(g => new V2UserSharesListItem
            {
                Id = g.Key,
                Image = g.First().SharedWithNavigation.GetImageUrl(),
                Name = g.First().SharedWithNavigation.Name,
                Shares = g.Select(y => new UserShareInfo
                    {
                        Id = y.Shocker.Id,
                        Name = y.Shocker.Name,
                        CreatedOn = y.CreatedOn,
                        Permissions = new ShockerPermissions
                        {
                            Sound = y.PermSound,
                            Vibrate = y.PermVibrate,
                            Shock = y.PermShock,
                            Live = y.PermLive
                        },
                        Limits = new ShockerLimits
                        {
                            Duration = y.LimitDuration,
                            Intensity = y.LimitIntensity
                        },
                        Paused = y.Paused
                    })
                    .ToArray()
            })
            .AsAsyncEnumerable();
    }
}