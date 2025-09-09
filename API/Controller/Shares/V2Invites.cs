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

file static class QueryHelper
{
    public static readonly System.Linq.Expressions.Expression<Func<UserShareInvite, ShareInviteBaseDetails>> SelectShareInvite = x =>
        new ShareInviteBaseDetails
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            Owner = new BasicUserInfo
            {
                Id = x.Owner.Id,
                Name = x.Owner.Name,
                Image = x.Owner.GetImageUrl()
            },
            SharedWith = x.RecipientUser == null
                ? null
                : new BasicUserInfo
                {
                    Id = x.RecipientUser.Id,
                    Name = x.RecipientUser.Name,
                    Image = x.RecipientUser.GetImageUrl()
                },
            Shockers = x.ShockerMappings.Select(y => new ShockerPermLimitPairWithId
            {
                Id = y.ShockerId,
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
        };
}

public sealed partial class SharesController
{
    [HttpGet("invites/outgoing")]
    [ApiVersion("2")]
    public IAsyncEnumerable<ShareInviteBaseDetails> GetOutgoingInvitesList()
    {
        return _db.UserShareInvites
            .Where(x => x.OwnerId == CurrentUser.Id)
            .Select(QueryHelper.SelectShareInvite)
            .AsAsyncEnumerable();
    }
    
    [HttpGet("invites/incoming")]
    [ApiVersion("2")]
    public IAsyncEnumerable<ShareInviteBaseDetails> GetIncomingInvitesList()
    {
        return _db.UserShareInvites
            .Where(x => x.RecipientUserId == CurrentUser.Id)
            .Select(QueryHelper.SelectShareInvite)
            .AsAsyncEnumerable();
    }
    
    [HttpDelete("invites/outgoing/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareRequestNotFound
    [ApiVersion("2")]
    public async Task<IActionResult> DeleteOutgoingInvite(Guid id)
    {
        var deletedShareRequest = await _db.UserShareInvites
            .Where(x => x.Id == id && x.OwnerId == CurrentUser.Id).ExecuteDeleteAsync();
        
        if (deletedShareRequest <= 0) return Problem(ShareError.ShareRequestNotFound);
        
        return Ok();
    }
    
    [HttpDelete("invites/incoming/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareRequestNotFound
    [ApiVersion("2")]
    public async Task<IActionResult> DenyIncomingInvite(Guid id)
    {
        var deletedShareRequest = await _db.UserShareInvites
            .Where(x => x.Id == id && x.RecipientUserId == CurrentUser.Id).ExecuteDeleteAsync();
        
        if (deletedShareRequest <= 0) return Problem(ShareError.ShareRequestNotFound);
        
        return Ok();
    }

    /// <summary>
    /// Accept a share request and share the shockers with the current user.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="deviceUpdateService"></param>
    /// <returns></returns>
    [HttpPost("invites/incoming/{id:guid}")]
    [ProducesResponseType<V2UserSharesListItem>(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareRequestNotFound
    [ApiVersion("2")]
    public async Task<IActionResult> RedeemInvite(Guid id, [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var shareRequest = await _db.UserShareInvites
            .Include(x => x.ShockerMappings).ThenInclude(x => x.Shocker).Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == id && (x.RecipientUserId == null || x.RecipientUserId == CurrentUser.Id));
        
        if (shareRequest is null) return Problem(ShareError.ShareRequestNotFound);
        
        var alreadySharedShockers = await _db.UserShares.Where(x => x.Shocker.Device.Owner.Id == shareRequest.OwnerId && x.SharedWithUserId == CurrentUser.Id).ToListAsync();
        
        foreach (var shareInvitationShocker in shareRequest.ShockerMappings)
        {
            var existingShare = alreadySharedShockers.FirstOrDefault(x => x.ShockerId == shareInvitationShocker.ShockerId);
            if (existingShare is not null)
            {
                existingShare.AllowShock = shareInvitationShocker.AllowShock;
                existingShare.AllowVibrate = shareInvitationShocker.AllowVibrate;
                existingShare.AllowSound = shareInvitationShocker.AllowSound;
                existingShare.AllowLiveControl = shareInvitationShocker.AllowLiveControl;
                existingShare.MaxIntensity = shareInvitationShocker.MaxIntensity;
                existingShare.MaxDuration = shareInvitationShocker.MaxDuration;
                existingShare.IsPaused = shareInvitationShocker.IsPaused;
            }
            else
            {
                var newShare = new UserShare
                {
                    SharedWithUserId = CurrentUser.Id,
                    ShockerId = shareInvitationShocker.ShockerId,
                    AllowShock = shareInvitationShocker.AllowShock,
                    AllowVibrate = shareInvitationShocker.AllowVibrate,
                    AllowSound = shareInvitationShocker.AllowSound,
                    AllowLiveControl = shareInvitationShocker.AllowLiveControl,
                    MaxIntensity = shareInvitationShocker.MaxIntensity,
                    MaxDuration = shareInvitationShocker.MaxDuration,
                    IsPaused = shareInvitationShocker.IsPaused
                };
                
                alreadySharedShockers.Add(newShare);
            }
        }
        
        _db.UserShareInvites.Remove(shareRequest);

        if (await _db.SaveChangesAsync() < 1) throw new Exception("Error while linking share code to your account");

        var affectedHubs = shareRequest.ShockerMappings.Select(x => x.ShockerId).Distinct();
        foreach (var affectedHub in affectedHubs)
        {
            await deviceUpdateService.UpdateDevice(shareRequest.OwnerId, affectedHub, DeviceUpdateType.ShockerUpdated, CurrentUser.Id);    
        }
        
        var returnObject = new V2UserSharesListItemDto()
        {
            Id = shareRequest.OwnerId,
            Email = shareRequest.Owner.Email,
            Name = shareRequest.Owner.Name,
            Shares = shareRequest.ShockerMappings.Select(y => new UserShareInfo
            {
                Id = y.Shocker.Id,
                Name = y.Shocker.Name,
                CreatedOn = DateTime.UtcNow,
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
        };

        
        
        return Ok(returnObject.ToV2UserSharesListItem());
    }
}

public class ShareRequestBase
{
    public required Guid Id { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required BasicUserInfo Owner { get; set; }
    public required BasicUserInfo? SharedWith { get; set; }
}

public sealed class ShareInviteBaseItem : ShareRequestBase
{
    public required ShareRequestCounts Counts { get; set; }
    
    public sealed class ShareRequestCounts
    {
        public required int Shockers { get; set; }
    }
}

public sealed class ShareInviteBaseDetails : ShareRequestBase
{
    public required IEnumerable<ShockerPermLimitPairWithId> Shockers { get; set; }
}