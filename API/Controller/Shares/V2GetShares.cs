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
        var sharedWithOthersFuture = _db.UserShares
            .Where(x => x.Shocker.Device.OwnerId == CurrentUser.Id)
            .AsNoTracking()
            .GroupBy(x => x.SharedWithUserId)
            .Select(g => new V2UserSharesListItemDto
            {
                Id = g.Key,
                Email = g.First().SharedWithUser.Email,
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
            .Future();

        var sharedWithMeFuture = _db.UserShares
            .Where(x => x.SharedWithUserId == CurrentUser.Id)
            .AsNoTracking()
            .GroupBy(x => x.Shocker.Device.OwnerId)
            .Select(g => new V2UserSharesListItemDto
            {
                Id = g.Key,
                Email = g.First().Shocker.Device.Owner.Email,
                Name = g.First().Shocker.Device.Owner.Name,
                Shares = g.Select(y => new UserShareInfo
                    {
                        Id = y.Shocker.Id,
                        Name = y.Shocker.Name,
                        CreatedOn = y.CreatedAt,
                        Permissions = new ShockerPermissions
                        {
                            Sound = y.AllowSound,
                            Vibrate = y.AllowVibrate,
                            Shock = y.AllowShock,
                            Live = y.AllowLiveControl
                        },
                        Limits = new ShockerLimits
                        {
                            Duration = y.MaxDuration,
                            Intensity = y.MaxIntensity
                        },
                        Paused = y.IsPaused
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