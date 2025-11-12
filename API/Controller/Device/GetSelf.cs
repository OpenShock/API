using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Device;

public sealed partial class DeviceController
{
    /// <summary>
    /// Gets information about the authenticated device.
    /// </summary>
    /// <response code="200">The device information was successfully retrieved.</response>
    [HttpGet("self")]
    [MapToApiVersion("1")]
    [ProducesResponseType<LegacyDataResponse<DeviceSelfResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> GetSelf()
    {
        var identity = User.GetOpenShockHubIdentity();
        var currentHubId = identity.GetClaimValueAsGuid(OpenShockAuthClaims.HubId);
        var currentHubName = identity.GetClaimValue(OpenShockAuthClaims.HubName);
        
        var shockers = await _db.Shockers.Where(x => x.DeviceId == currentHubId).Select(x => new MinimalShocker
        {
            Id = x.Id,
            RfId = x.RfId,
            Model = x.Model
        }).ToArrayAsync();

        return LegacyDataOk(new DeviceSelfResponse
            {
                Id = currentHubId,
                Name = currentHubName,
                Shockers = shockers
            }
        );
    }
}