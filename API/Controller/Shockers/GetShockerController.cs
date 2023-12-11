using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Gets information about a shocker.
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">The shocker information was successfully retrieved.</response>
    /// <response code="404">The shocker does not exist or you do not have access to it.</response>
    [HttpGet("{id}", Name = "GetShocker")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<ShockerWithDevice>> GetShocker([FromRoute] Guid id)
    {
        var shocker = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id).Select(x => new ShockerWithDevice
        {
            Id = x.Id,
            Name = x.Name,
            RfId = x.RfId,
            CreatedOn = x.CreatedOn,
            Device = x.Device,
            Model = x.Model,
            IsPaused = x.Paused
        }).SingleOrDefaultAsync();

        if (shocker == null)
            return EBaseResponse<ShockerWithDevice>("Shocker does not exist", HttpStatusCode.NotFound);
        return new BaseResponse<ShockerWithDevice>
        {
            Data = shocker
        };
    }
}