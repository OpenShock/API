using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Redis;
using Redis.OM;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Device;

public sealed partial class DeviceController
{
    /// <summary>
    /// Pair a device with a pair code.
    /// </summary>
    /// <param name="pairCode">The pair code to pair with.</param>
    /// <response code="200">Successfully assigned LCG node</response>
    /// <response code="404">No such pair code exists</response>
    [AllowAnonymous]
    [MapToApiVersion("1")]
    [HttpGet("pair/{pairCode}", Name = "Pair")]
    [HttpGet("~/{version:apiVersion}/pair/{pairCode}", Name = "Pair_DEPRECATED")] // Backwards compatibility
    [EnableRateLimiting("auth")]
    [ProducesResponseType<LegacyDataResponse<string>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PairCodeNotFound
    public async Task<IActionResult> Pair([FromRoute] string pairCode)
    {
        var devicePairs = _redis.RedisCollection<DevicePair>();

        var pair = await devicePairs.FirstOrDefaultAsync(x => x.PairCode == pairCode);
        if (pair is null) return Problem(PairError.PairCodeNotFound);
        await devicePairs.DeleteAsync(pair);

        var deviceToken = await _db.Devices.Where(x => x.Id == pair.Id).Select(x => x.Token).FirstOrDefaultAsync();
        if (deviceToken is null) throw new Exception("Device not found for pair code");

        return LegacyDataOk(deviceToken);
    }
}