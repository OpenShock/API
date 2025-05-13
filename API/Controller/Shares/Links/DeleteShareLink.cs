using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;
using OpenShock.Common.Extensions;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Deletes a share link
    /// </summary>
    /// <param name="shareLinkId"></param>
    /// <response code="200">Deleted share link</response>
    /// <response code="404">Share link not found or does not belong to you</response>
    [HttpDelete("{shareLinkId}")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareLinkNotFound
    public async Task<IActionResult> DeleteShareLink([FromRoute] Guid shareLinkId)
    {
        var result = await _db.ShockerShareLinks
            .Where(x => x.Id == shareLinkId)
            .WhereIsUserOrPrivileged(x => x.Owner, CurrentUser)
            .ExecuteDeleteAsync();

        return result > 0
            ? LegacyEmptyOk("Deleted share link")
            : Problem(ShareLinkError.ShareLinkNotFound);
    }
}