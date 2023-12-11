using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using System.Net;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Deletes a share link
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">Deleted share link</response>
    /// <response code="404">Share link not found or does not belong to you</response>
    [HttpDelete("{id}", Name = "DeleteShareLink")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> DeleteShareLink([FromRoute] Guid id)
    {
        var result = await _db.ShockerSharesLinks.Where(x => x.Id == id && x.OwnerId == CurrentUser.DbUser.Id)
            .ExecuteDeleteAsync();

        return result > 0
            ? new BaseResponse<object>("Successfully deleted share link")
            : EBaseResponse<object>("Share link not found or does not belong to you", HttpStatusCode.NotFound);
    }
}