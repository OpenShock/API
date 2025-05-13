using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Get all share links for the current user
    /// </summary>
    /// <response code="200">All share links for the current user</response>
    [HttpGet]
    public LegacyDataResponse<IAsyncEnumerable<ShareLinkResponse>> List()
    {
        var ownShareLinks = _db.ShockerShareLinks
            .Where(x => x.OwnerId == CurrentUser.Id)
            .Select(x => ShareLinkResponse.GetFromEf(x))
            .AsAsyncEnumerable();

        return new(ownShareLinks);
    }
}