using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Get all share links for the current user
    /// </summary>
    /// <response code="200">All share links for the current user</response>
    [HttpGet]
    [ProducesSuccess<IEnumerable<ShareLinkResponse>>]
    public async Task<BaseResponse<IEnumerable<ShareLinkResponse>>> List()
    {
        var ownShareLinks = await _db.ShockerSharesLinks.Where(x => x.OwnerId == CurrentUser.DbUser.Id)
            .Select(x => ShareLinkResponse.GetFromEf(x)).ToListAsync();

        return new BaseResponse<IEnumerable<ShareLinkResponse>>
        {
            Data = ownShareLinks
        };
    }
}