using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Models.Services.Ota;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Ota;

namespace OpenShock.API.Controller.Devices;

[ApiController]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.UserSessionCookie)]
[ApiVersion("1")]
[Route("/{version:apiVersion}/devices")]
public sealed class DevicesOtaController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<DevicesController> _logger;

    public DevicesOtaController(OpenShockContext db, ILogger<DevicesController> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets the OTA update history for a device
    /// </summary>
    /// <param name="deviceId">Id of the device</param>
    /// <param name="otaService"></param>
    /// <response code="200">OK</response>
    /// <response code="404">Could not find device or you do not have access to it</response>
    [HttpGet("{deviceId}/ota")]
    [MapToApiVersion("1")]
    [ProducesResponseType<LegacyDataResponse<IReadOnlyCollection<OtaItem>>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // DeviceNotFound
    public async Task<IActionResult> GetOtaUpdateHistory([FromRoute] Guid deviceId, [FromServices] IOtaService otaService)
    {
        // Check if user owns device or has a share
        var deviceExistsAndYouHaveAccess = await _db.Devices.AnyAsync(x =>
            x.Id == deviceId && x.Owner == CurrentUser.Id);
        if (!deviceExistsAndYouHaveAccess) return Problem(DeviceError.DeviceNotFound);

        return LegacyDataOk(await otaService.GetUpdates(deviceId));
    }
    
}