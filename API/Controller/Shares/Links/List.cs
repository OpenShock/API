using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Get all public shares for the current user
    /// </summary>
    /// <response code="200">All public shares for the current user</response>
    [HttpGet]
    [ProducesResponseType<LegacyDataResponse<OwnPublicShareResponse[]>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult List()
    {
        var ownPublicShares = _db.PublicShares
            .Where(x => x.OwnerId == CurrentUser.Id)
            .Select(x => OwnPublicShareResponse.GetFromEf(x))
            .AsAsyncEnumerable();

        return LegacyDataOk(ownPublicShares);
    }
}