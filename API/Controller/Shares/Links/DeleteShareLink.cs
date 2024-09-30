using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.ServicesCommon.Errors;
using OpenShock.ServicesCommon.Problems;
using System.Net;

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
    [ProducesSuccess]
    [ProducesProblem(HttpStatusCode.NotFound, "ShareLinkNotFound")]
    public async Task<IActionResult> DeleteShareLink([FromRoute] Guid shareLinkId)
    {
        var result = await _db.ShockerSharesLinks.Where(x => x.Id == shareLinkId && x.OwnerId == CurrentUser.DbUser.Id)
            .ExecuteDeleteAsync();

        return result > 0
            ? RespondSuccessSimple("Deleted share link")
            : Problem(ShareLinkError.ShareLinkNotFound);
    }
}