using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
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
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareCodeNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> DeleteShareCode([FromRoute] Guid shareCodeId)
    {
        var affected = await _db.ShockerShareCodes
            .Where(x => x.Id == shareCodeId)
            .WhereIsUserOrPrivileged(x => x.Shocker.DeviceNavigation.OwnerNavigation, CurrentUser)
            .ExecuteDeleteAsync();
        if (affected <= 0)
        {
            return Problem(ShareCodeError.ShareCodeNotFound);
        }

        return LegacyEmptyOk("Successfully deleted share code");
    }
}