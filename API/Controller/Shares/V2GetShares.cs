using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Extensions;
using OpenShock.Common.Utils;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Controller.Shares;

internal sealed class V2UserSharesListItemDto
{

    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; init; }
    public required IEnumerable<UserShareInfo> Shares { get; init; }
    
    public V2UserSharesListItem ToV2UserSharesListItem() => new()
    {
        Id = Id,
        Name = Name,
        Image = GravatarUtils.GetUserImageUrl(Email),
        Shares = Shares
    };
}

public sealed partial class SharesController
{
    [HttpGet]
    [ApiVersion("2")]
    public async Task<V2UserShares> GetSharesByUsers(CancellationToken cancellationToken)
    {
        var outgoingSharesFuture = _db.UserShares
            .AsNoTracking()
            .Where(x => x.Shocker.Device.OwnerId == CurrentUser.Id)
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

        var incomingSharesFuture = _db.UserShares
            .AsNoTracking()
            .Where(x => x.SharedWithUserId == CurrentUser.Id && x.SharedWithUser.UserDeactivation == null)
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
            Outgoing = (await outgoingSharesFuture.ToArrayAsync(cancellationToken)).Select(x => x.ToV2UserSharesListItem()),
            Incoming = (await incomingSharesFuture.ToArrayAsync(cancellationToken)).Select(x => x.ToV2UserSharesListItem())
        };
    }

    public sealed class V2UserShares
    {
        public required IEnumerable<V2UserSharesListItem> Outgoing { get; set; }
        public required IEnumerable<V2UserSharesListItem> Incoming { get; set; }
    }
}