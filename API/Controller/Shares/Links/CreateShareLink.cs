using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Net;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Creates a new share link
    /// </summary>
    /// <param name="data"></param>
    /// <response code="200">The created share link</response>
    [HttpPost(Name = "CreateShareLink")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<Guid>> CreateShareLink([FromBody] ShareLinkCreate data)
    {
        var entity = new ShockerSharesLink
        {
            Id = Guid.NewGuid(),
            Owner = CurrentUser.DbUser,
            ExpiresOn = data.ExpiresOn == null ? null : DateTime.SpecifyKind(data.ExpiresOn.Value, DateTimeKind.Utc),
            Name = data.Name
        };
        _db.ShockerSharesLinks.Add(entity);
        await _db.SaveChangesAsync();

        return new BaseResponse<Guid>
        {
            Data = entity.Id
        };
    }
}