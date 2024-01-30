using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using System.Net;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Get all share links for the current user
    /// </summary>
    /// <response code="200">All share links for the current user</response>
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<IEnumerable<ShareLinkResponse>>> ListShareLinks()
    {
        var ownShareLinks = await _db.ShockerSharesLinks.Where(x => x.OwnerId == CurrentUser.DbUser.Id)
            .Select(x => ShareLinkResponse.GetFromEf(x)).ToListAsync();

        return new BaseResponse<IEnumerable<ShareLinkResponse>>
        {
            Data = ownShareLinks
        };
    }
}