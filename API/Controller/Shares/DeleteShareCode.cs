using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Shares;

public sealed partial class SharesController
{
    /// <summary>
    /// Delete a share code
    /// </summary>
    /// <param name="shareCodeId"></param>
    /// <response code="200">Deleted share code</response>
    /// <response code="404">Share code not found or does not belong to you</response>
    [HttpDelete("code/{shareCodeId}")]
    [ProducesSuccess]
    [ProducesProblem(HttpStatusCode.NotFound, "ShareCodeNotFound")]
    public async Task<IActionResult> DeleteShareCode([FromRoute] Guid shareCodeId)
    {
        var yes = await _db.ShockerShareCodes
            .Where(x => x.Id == shareCodeId && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).FirstOrDefaultAsync();
        var affected = await _db.ShockerShareCodes.Where(x =>
            x.Id == shareCodeId && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).ExecuteDeleteAsync();
        if (affected <= 0) return Problem(ShareCodeError.ShareCodeNotFound);

        return RespondSuccessSimple("Successfully deleted share code");
    }
}