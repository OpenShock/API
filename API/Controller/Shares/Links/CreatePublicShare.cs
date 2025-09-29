using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Create a new public share
    /// </summary>
    /// <response code="200">The created public share</response>
    [HttpPost(Name = "CreatePublicShare")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LegacyDataResponse<Guid>>(StatusCodes.Status200OK,  MediaTypeNames.Application.Json)]
    public async Task<IActionResult> CreatePublicShare([FromBody] PublicShareCreate body)
    {
        var entity = new PublicShare
        {
            Id = Guid.CreateVersion7(),
            OwnerId = CurrentUser.Id,
            Name = body.Name,
            ExpiresAt = body.ExpiresOn is null ? null : DateTime.SpecifyKind(body.ExpiresOn.Value, DateTimeKind.Utc)
        };
        _db.PublicShares.Add(entity);
        await _db.SaveChangesAsync();

        return LegacyDataOk(entity.Id);
    }
}