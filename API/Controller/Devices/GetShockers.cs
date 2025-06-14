﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using System.Net.Mime;
using Asp.Versioning;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Devices;

public sealed partial class DevicesController
{
    /// <summary>
    /// Get all shockers for a device
    /// </summary>
    /// <param name="deviceId">The device id</param>
    /// <response code="200">All shockers for the device</response>
    /// <response code="404">Device does not exists or you do not have access to it.</response>
    [HttpGet("{deviceId}/shockers")]
    [ProducesResponseType<LegacyDataResponse<IAsyncEnumerable<ShockerResponse>>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // DeviceNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetShockers([FromRoute] Guid deviceId)
    {
        var deviceExists = await _db.Devices.AnyAsync(x => x.OwnerId == CurrentUser.Id && x.Id == deviceId);
        if (!deviceExists) return Problem(HubError.HubNotFound);
        
        var shockers = _db.Shockers
            .Where(x => x.DeviceId == deviceId)
            .Select(x => new ShockerResponse
            {
                Id = x.Id,
                Name = x.Name,
                RfId = x.RfId,
                CreatedOn = x.CreatedAt,
                Model = x.Model,
                IsPaused = x.IsPaused
            })
            .AsAsyncEnumerable();

        return LegacyDataOk(shockers);
    }
}