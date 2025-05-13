using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Shares;

public sealed partial class SharesController
{
    [HttpGet("requests/outgoing")]
    [ApiVersion("2")]
    public IAsyncEnumerable<ShareRequestBaseDetails> GetOutgoingRequestsList()
    {
        return _db.ShareRequests.Where(x => x.Owner == CurrentUser.Id)
            .Select(x => new ShareRequestBaseDetails
            {
                Id = x.Id,
                CreatedOn = x.CreatedAt,
                Owner = new GenericIni
                {
                    Id = x.OwnerNavigation.Id,
                    Name = x.OwnerNavigation.Name,
                    Image = x.OwnerNavigation.GetImageUrl()
                },
                SharedWith = x.UserNavigation == null
                    ? null
                    : new GenericIni
                    {
                        Id = x.UserNavigation.Id,
                        Name = x.UserNavigation.Name,
                        Image = x.UserNavigation.GetImageUrl()
                    },
                Shockers = x.ShareRequestsShockers.Select(y => new ShockerPermLimitPairWithId
                {
                    Id = y.Shocker,
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
                    }
                })
            }).AsAsyncEnumerable();
    }
    
    [HttpGet("requests/incoming")]
    [ApiVersion("2")]
    public IAsyncEnumerable<ShareRequestBaseDetails> GetIncomingRequestsList()
    {
        return _db.ShareRequests.Where(x => x.User == CurrentUser.Id)
            .Select(x => new ShareRequestBaseDetails
            {
                Id = x.Id,
                CreatedOn = x.CreatedAt,
                Owner = new GenericIni
                {
                    Id = x.OwnerNavigation.Id,
                    Name = x.OwnerNavigation.Name,
                    Image = x.OwnerNavigation.GetImageUrl()
                },
                SharedWith = x.UserNavigation == null
                    ? null
                    : new GenericIni
                    {
                        Id = x.UserNavigation.Id,
                        Name = x.UserNavigation.Name,
                        Image = x.UserNavigation.GetImageUrl()
                    },
                Shockers = x.ShareRequestsShockers.Select(y => new ShockerPermLimitPairWithId
                {
                    Id = y.Shocker,
                    Limits = new ShockerLimits
                    {
                        Duration = y.MaxDuration,
                        Intensity = y.MaxIntensity
                    },
                    Permissions = new ShockerPermissions
                    {
                        Vibrate = y.AllowVibrate,
                        Sound = y.AllowSound,
                        Shock = y.AllowShock,
                        Live = y.AllowLiveControl
                    }
                })
            }).AsAsyncEnumerable();
    }
    
    [HttpDelete("requests/outgoing/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareRequestNotFound
    [ApiVersion("2")]
    public async Task<IActionResult> DeleteRequest(Guid id)
    {
        var deletedShareRequest = await _db.ShareRequests
            .Where(x => x.Id == id && x.Owner == CurrentUser.Id).ExecuteDeleteAsync();
        
        if (deletedShareRequest <= 0) return Problem(ShareError.ShareRequestNotFound);
        
        return Ok();
    }
    
    [HttpDelete("requests/incoming/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareRequestNotFound
    [ApiVersion("2")]
    public async Task<IActionResult> DenyRequest(Guid id)
    {
        var deletedShareRequest = await _db.ShareRequests
            .Where(x => x.Id == id && x.User == CurrentUser.Id).ExecuteDeleteAsync();
        
        if (deletedShareRequest <= 0) return Problem(ShareError.ShareRequestNotFound);
        
        return Ok();
    }

    /// <summary>
    /// Accept a share request and share the shockers with the current user.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="deviceUpdateService"></param>
    /// <returns></returns>
    [HttpPost("requests/incoming/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareRequestNotFound
    [ApiVersion("2")]
    public async Task<IActionResult> RedeemRequest(Guid id, [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var shareRequest = await _db.ShareRequests
            .Where(x => x.Id == id && (x.User == null || x.User == CurrentUser.Id)).Include(x => x.ShareRequestsShockers).FirstOrDefaultAsync();
        
        if (shareRequest == null) return Problem(ShareError.ShareRequestNotFound);
        
        var alreadySharedShockers = await _db.ShockerShares.Where(x => x.Shocker.DeviceNavigation.OwnerNavigation.Id == shareRequest.Owner && x.SharedWith == CurrentUser.Id).ToListAsync();
        
        foreach (var shareRequestShocker in shareRequest.ShareRequestsShockers)
        {
            var existingShare = alreadySharedShockers.FirstOrDefault(x => x.ShockerId == shareRequestShocker.Shocker);
            if (existingShare != null)
            {
                existingShare.AllowShock = shareRequestShocker.AllowShock;
                existingShare.AllowVibrate = shareRequestShocker.AllowVibrate;
                existingShare.AllowSound = shareRequestShocker.AllowSound;
                existingShare.AllowLiveControl = shareRequestShocker.AllowLiveControl;
                existingShare.MaxIntensity = shareRequestShocker.MaxIntensity;
                existingShare.MaxDuration = shareRequestShocker.MaxDuration;
                existingShare.IsPaused = shareRequestShocker.IsPaused;
            }
            else
            {
                var newShare = new ShockerShare
                {
                    ShockerId = shareRequestShocker.Shocker,
                    SharedWith = CurrentUser.Id,
                    AllowShock = shareRequestShocker.AllowShock,
                    AllowVibrate = shareRequestShocker.AllowVibrate,
                    AllowSound = shareRequestShocker.AllowSound,
                    AllowLiveControl = shareRequestShocker.AllowLiveControl,
                    MaxIntensity = shareRequestShocker.MaxIntensity,
                    MaxDuration = shareRequestShocker.MaxDuration,
                    IsPaused = shareRequestShocker.IsPaused
                };
                
                alreadySharedShockers.Add(newShare);
            }
        }
        
        _db.ShareRequests.Remove(shareRequest);

        if (await _db.SaveChangesAsync() < 1) throw new Exception("Error while linking share code to your account");

        var affectedHubs = shareRequest.ShareRequestsShockers.Select(x => x.Shocker).Distinct();
        foreach (var affectedHub in affectedHubs)
        {
            await deviceUpdateService.UpdateDevice(shareRequest.Owner, affectedHub, DeviceUpdateType.ShockerUpdated, CurrentUser.Id);    
        }
        
        return Ok();
    }
}

public class ShareRequestBase
{
    public required Guid Id { get; set; }
    public required DateTime CreatedOn { get; set; }
    public required GenericIni Owner { get; set; }
    public required GenericIni? SharedWith { get; set; }
}

public sealed class ShareRequestBaseItem : ShareRequestBase
{
    public required ShareRequestCounts Counts { get; set; }
    
    public sealed class ShareRequestCounts
    {
        public required int Shockers { get; set; }
    }
}

public sealed class ShareRequestBaseDetails : ShareRequestBase
{
    public required IEnumerable<ShockerPermLimitPairWithId> Shockers { get; set; }
}