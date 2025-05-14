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
    public async Task<LegacyDataResponse<Guid>> CreatePublicShare([FromBody] PublicShareCreate body)
    {
        var entity = new PublicShare
        {
            Id = Guid.CreateVersion7(),
            Owner = CurrentUser,
            ExpiresAt = body.ExpiresOn == null ? null : DateTime.SpecifyKind(body.ExpiresOn.Value, DateTimeKind.Utc),
            Name = body.Name
        };
        _db.PublicShares.Add(entity);
        await _db.SaveChangesAsync();

        return new(entity.Id);
    }
}