using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.Models.Services.Ota;
using OpenShock.ServicesCommon.Services.Ota;

namespace OpenShock.API.Controller.Devices;

public sealed partial class DevicesController
{
    [HttpGet("{id}/ota", Name = "GetOtaUpdates")]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<IReadOnlyCollection<OtaItem>>> GetOtaUpdates([FromRoute] Guid id, [FromServices] IOtaService otaService)
    {
        // Check if user owns device or has a share
        var deviceExistsAndYouHaveAccess = await _db.Devices.AnyAsync(x =>
            x.Id == id && x.Owner == CurrentUser.DbUser.Id);
        if (!deviceExistsAndYouHaveAccess)
            return EBaseResponse<IReadOnlyCollection<OtaItem>>("Device does not exists or does not belong to you",
                HttpStatusCode.NotFound);

        return new BaseResponse<IReadOnlyCollection<OtaItem>>
        {
            Data = await otaService.GetUpdates(id)
        };
    }
    
}