using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.API.Utils;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;

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
    [Tags("Shocker ShareLinks")]
    [ProducesResponseType<LegacyDataResponse<PublicShareLinkResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareLinkNotFound
    public async Task<IActionResult> GetShareLink([FromRoute] Guid shareLinkId)
    {
        var shareLink = await _db.ShockerShareLinks.Where(x => x.Id == shareLinkId).Select(x => new
        {
            Author = new GenericIni
            {
                Id = x.Owner.Id,
                Name = x.Owner.Name,
                Image = x.Owner.GetImageUrl()
            },
            x.Id,
            x.Name,
            x.ExpiresAt,
            x.CreatedAt,
            Shockers = x.ShockerMappings.Select(y => new
            {
                DeviceId = y.Shocker.Device.Id,
                DeviceName = y.Shocker.Device.Name,
                Shocker = new ShareLinkShocker
                {
                    Id = y.Shocker.Id,
                    Name = y.Shocker.Name,
                    Limits = new ShockerLimits
                    {
                        Intensity = y.MaxIntensity,
                        Duration = y.MaxDuration
                    },
                    Permissions = new ShockerPermissions
                    {
                        Vibrate = y.AllowVibrate,
                        Sound = y.AllowSound,
                        Shock = y.AllowShock,
                        Live = y.AllowLiveControl
                    },
                    Paused = ShareLinkUtils.GetPausedReason(y.IsPaused, y.Shocker.IsPaused),
                }
            })
        }).FirstOrDefaultAsync();

        if (shareLink == null) return Problem(ShareLinkError.ShareLinkNotFound);
        
        
        var final = new PublicShareLinkResponse
        {
            Id = shareLink.Id,
            Name = shareLink.Name,
            Author = shareLink.Author,
            CreatedOn = shareLink.CreatedAt,
            ExpiresOn = shareLink.ExpiresAt
        };
        foreach (var shocker in shareLink.Shockers)
        {
            if (final.Devices.All(x => x.Id != shocker.DeviceId))
                final.Devices.Add(new ShareLinkDevice
                {
                    Id = shocker.DeviceId,
                    Name = shocker.DeviceName,
                    Shockers = []
                });

            final.Devices.Single(x => x.Id == shocker.DeviceId).Shockers.Add(shocker.Shocker);
        }

        return LegacyDataOk(final);
    }
}