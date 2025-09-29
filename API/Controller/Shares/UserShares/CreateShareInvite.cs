using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Controller.Shares.UserShares;

public sealed partial class UserSharesController
{
    [HttpPost("invites")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK, MediaTypeNames.Text.Plain)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // UserNotFound, ShareCreateShockerNotFound
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // ShareCreateCannotShareWithSelf
    [MapToApiVersion("2")]
    public async Task<IActionResult> CreateShareInvite([FromBody] CreateShareRequest body)
    {
        if (body.User == CurrentUser.Id)
        {
            return Problem(ShareError.ShareRequestCreateCannotShareWithSelf);
        }
        
        var providedShockerIds = body.Shockers.Select(x => x.Id).ToArray();
        var belongsToUsFuture = _db.Shockers.AsNoTracking().Where(x =>
            x.Device.OwnerId == CurrentUser.Id && providedShockerIds.Contains(x.Id)).Select(x => x.Id).Future();
        
        if (body.User is not null)
        {
            var existsFuture = _db.Users.AsNoTracking().DeferredAny(x => x.Id == body.User).FutureValue();
            
            // We can already resolve the futures here since this is the last future query
            if (!await existsFuture.ValueAsync()) return Problem(UserError.UserNotFound);
        }

        var belongsToUs = await belongsToUsFuture.ToArrayAsync();

        var missingShockers = providedShockerIds.Except(belongsToUs).ToArray();
        
        if (missingShockers.Length > 0)
        {
            return Problem(ShareError.ShareCreateShockerNotFound(missingShockers));
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var shareInvite = new UserShareInvite
        {
            Id = Guid.CreateVersion7(),
            OwnerId = CurrentUser.Id,
            RecipientUserId = body.User
        };
        _db.UserShareInvites.Add(shareInvite);
        
        foreach (var createUserShare in body.Shockers)
        {
            _db.UserShareInviteShockers.Add(new UserShareInviteShocker
            {
                InviteId = shareInvite.Id,
                ShockerId = createUserShare.Id,
                AllowShock = createUserShare.Permissions.Shock,
                AllowVibrate = createUserShare.Permissions.Vibrate,
                AllowSound = createUserShare.Permissions.Sound,
                AllowLiveControl = createUserShare.Permissions.Live,
                MaxIntensity = createUserShare.Limits.Intensity,
                MaxDuration = createUserShare.Limits.Duration,
                IsPaused = false
            });
        }

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(shareInvite.Id);
    }
}