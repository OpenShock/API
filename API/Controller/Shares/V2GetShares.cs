using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Extensions;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Controller.Shares;

public sealed partial class SharesController
{
    [HttpGet]
    [ApiVersion("2")]
    public async Task<V2UserShares> GetSharesByUsers(CancellationToken cancellationToken)
    {
        var sharedWithOthersFuture = _db.ShockerShares
            .Where(x => x.Shocker.DeviceNavigation.Owner == CurrentUser.Id)
            .AsNoTracking()
            .GroupBy(x => x.SharedWith)
            .Select(g => new V2UserSharesListItemDto
            {
                Id = g.Key,
                Email = g.First().SharedWithNavigation.Email,
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
            .Future();

        var sharedWithMeFuture = _db.ShockerShares
            .Where(x => x.SharedWith == CurrentUser.Id)
            .AsNoTracking()
            .GroupBy(x => x.Shocker.DeviceNavigation.Owner)
            .Select(g => new V2UserSharesListItemDto
            {
                Id = g.Key,
                Email = g.First().Shocker.DeviceNavigation.OwnerNavigation.Email,
                Name = g.First().Shocker.DeviceNavigation.OwnerNavigation.Name,
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
            .Future();

        return new V2UserShares
        {
            SharedWithMe = (await sharedWithMeFuture.ToArrayAsync(cancellationToken)).Select(x => x.FromDto()),
            SharedWithOthers = (await sharedWithOthersFuture.ToArrayAsync(cancellationToken)).Select(x => x.FromDto())
        };
    }

    public sealed class V2UserShares
    {
        public required IEnumerable<V2UserSharesListItem> SharedWithMe { get; set; }
        public required IEnumerable<V2UserSharesListItem> SharedWithOthers { get; set; }
    }
}