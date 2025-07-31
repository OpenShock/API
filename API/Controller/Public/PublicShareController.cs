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
    /// Gets information about a public share.
    /// </summary>
    /// <param name="publicShareId"></param>
    /// <response code="200">The public share information was successfully retrieved.</response>
    /// <response code="404">The public share does not exist.</response>
    [HttpGet("shares/links/{publicShareId}")]
    [Tags("Public Shocker Shares")]
    [ProducesResponseType<LegacyDataResponse<PublicShareResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PublicShareNotFound
    public async Task<IActionResult> GetPublicShare([FromRoute] Guid publicShareId)
    {
        var publicShare = await _db.PublicShares
            .Where(x => x.Id == publicShareId && x.Owner.UserDeactivation != null)
            .Select(x => new
        {
            Author = new BasicUserInfo
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
                Shocker = new PublicShareShocker
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
                    Paused = PublicShareUtils.GetPausedReason(y.IsPaused, y.Shocker.IsPaused),
                }
            })
        }).FirstOrDefaultAsync();

        if (publicShare is null) return Problem(PublicShareError.PublicShareNotFound);
        
        
        var final = new PublicShareResponse
        {
            Id = publicShare.Id,
            Name = publicShare.Name,
            Author = publicShare.Author,
            CreatedOn = publicShare.CreatedAt,
            ExpiresOn = publicShare.ExpiresAt
        };
        foreach (var shocker in publicShare.Shockers)
        {
            if (final.Devices.All(x => x.Id != shocker.DeviceId))
                final.Devices.Add(new PublicShareDevice
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