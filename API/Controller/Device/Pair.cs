using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using Redis.OM;
using Redis.OM.Contracts;
using System.Net;

namespace OpenShock.API.Controller.Device;

partial class DeviceController
{
    /// <summary>
    /// Pair a device with a pair code.
    /// </summary>
    /// <param name="pairCode">The pair code to pair with.</param>
    /// <response code="200">Successfully assigned LCG node</response>
    /// <response code="404">No such pair code exists</response>
    [AllowAnonymous]
    [HttpGet("pair/{pairCode}", Name = "PairDevice")]
    [HttpGet("~/{version:apiVersion}/pair/{pairCode}", Name = "PairDeviceLegacy")] // Backwards compatibility
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<string>> Pair([FromRoute] string pairCode)
    {
        var devicePairs = _redis.RedisCollection<DevicePair>();

        var pair = await devicePairs.Where(x => x.PairCode == pairCode).SingleOrDefaultAsync();
        if (pair == null) return EBaseResponse<string>("No such pair code exists", HttpStatusCode.NotFound);
        await devicePairs.DeleteAsync(pair);

        var device = await _db.Devices.SingleOrDefaultAsync(x => x.Id == pair.Id);
        if (device == null) return EBaseResponse<string>("No such device exists for the pair code", HttpStatusCode.InternalServerError);

        return new BaseResponse<string>
        {
            Data = device.Token
        };
    }
}