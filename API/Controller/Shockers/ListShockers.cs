using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// List all shockers belonging to the authenticated user.
    /// </summary>
    /// <response code="200">The shockers were successfully retrieved.</response>
    [HttpGet("own")]
    [MapToApiVersion("1")]
    public LegacyDataResponse<IAsyncEnumerable<ResponseDeviceWithShockers>> ListShockers()
    {
        var shockers = _db.Devices
            .Where(x => x.Owner == CurrentUser.Id)
            .OrderBy(x => x.CreatedAt).Select(x => new ResponseDeviceWithShockers
            {
                Id = x.Id,
                Name = x.Name,
                CreatedOn = x.CreatedAt,
                Shockers = x.Shockers
                    .OrderBy(y => y.CreatedAt)
                    .Select(y => new ShockerResponse
                    {
                        Id = y.Id,
                        Name = y.Name,
                        RfId = y.RfId,
                        CreatedOn = y.CreatedAt,
                        Model = y.Model,
                        IsPaused = y.IsPaused
                    })
                    .ToArray()
            })
            .AsAsyncEnumerable();

        return new(shockers);
    }
}