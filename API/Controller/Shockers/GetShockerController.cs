using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    [HttpGet("{id:guid}")]
    public async Task<BaseResponse<ShockerWithDevice>> GetShocker(Guid id)
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