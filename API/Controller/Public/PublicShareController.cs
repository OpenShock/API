﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Errors;
using OpenShock.ServicesCommon.Problems;
using OpenShock.ServicesCommon.Utils;
using System.Net;

namespace OpenShock.API.Controller.Public;

public sealed partial class PublicController
{
    /// <summary>
    /// Gets information about a public share link.
    /// </summary>
    /// <param name="shareLinkId"></param>
    /// <response code="200">The share link information was successfully retrieved.</response>
    /// <response code="404">The share link does not exist.</response>
    [HttpGet("shares/links/{shareLinkId}")]
    [ProducesSuccess<PublicShareLinkResponse>]
    [ProducesProblem(HttpStatusCode.NotFound, "ShareLinkNotFound")]
    public async Task<IActionResult> GetShareLink([FromRoute] Guid shareLinkId)
    {
        var shareLink = await _db.ShockerSharesLinks.Where(x => x.Id == shareLinkId).Select(x => new
        {
            Author = new GenericIni
            {
                Id = x.Owner.Id,
                Name = x.Owner.Name,
                Image = GravatarUtils.GetImageUrl(x.Owner.Email)
            },
            x.Id,
            x.Name,
            x.ExpiresOn,
            x.CreatedOn,
            Shockers = x.ShockerSharesLinksShockers.Select(y => new
            {
                DeviceId = y.Shocker.DeviceNavigation.Id,
                DeviceName = y.Shocker.DeviceNavigation.Name,
                Shocker = new ShareLinkShocker
                {
                    Id = y.Shocker.Id,
                    Name = y.Shocker.Name,
                    Limits = new ShockerLimits
                    {
                        Duration = y.LimitDuration,
                        Intensity = y.LimitIntensity
                    },
                    Permissions = new ShockerPermissions
                    {
                        Vibrate = y.PermVibrate,
                        Sound = y.PermSound,
                        Shock = y.PermShock,
                        Live = y.PermLive
                    },
                    Paused = ShareLinkUtils.GetPausedReason(y.Paused, y.Shocker.Paused),
                }
            })
        }).SingleOrDefaultAsync();

        if (shareLink == null) return RespondSuccess(ShareLinkError.ShareLinkNotFound);


        var final = new PublicShareLinkResponse
        {
            Id = shareLink.Id,
            Name = shareLink.Name,
            Author = shareLink.Author,
            CreatedOn = shareLink.CreatedOn,
            ExpiresOn = shareLink.ExpiresOn
        };
        foreach (var shocker in shareLink.Shockers)
        {
            if (final.Devices.All(x => x.Id != shocker.DeviceId))
                final.Devices.Add(new ShareLinkDevice
                {
                    Id = shocker.DeviceId,
                    Name = shocker.DeviceName,
                });

            final.Devices.Single(x => x.Id == shocker.DeviceId).Shockers.Add(shocker.Shocker);
        }

        return RespondSuccess(final);
    }
}