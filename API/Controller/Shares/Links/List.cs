using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Get all public shares for the current user
    /// </summary>
    /// <response code="200">All public shares for the current user</response>
    [HttpGet]
    [TokenPermission(PermissionType.Publicshares_Edit)]
    [ProducesResponseType<LegacyDataResponse<OwnPublicShareResponse[]>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult List()
    {
        var ownPublicShares = _db.PublicShares
            .Where(x => x.OwnerId == CurrentUser.Id && (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow))
            .Select(x => OwnPublicShareResponse.GetFromEf(x))
            .AsAsyncEnumerable();

        return LegacyDataOk(ownPublicShares);
    }
}