using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using OpenShock.ServicesCommon.Services.RedisPubSub;

namespace OpenShock.API.Controller.Devices;

public sealed partial class DevicesController
{
    /// <summary>
    /// Get all devices for the current user
    /// </summary>
    /// <response code="200">All devices for the current user</response>
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<IEnumerable<Models.Response.ResponseDevice>>> ListDevices()
    {
        var devices = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id)
            .Select(x => new Models.Response.ResponseDevice
            {
                Id = x.Id,
                Name = x.Name,
                CreatedOn = x.CreatedOn
            }).ToListAsync();
        return new BaseResponse<IEnumerable<Models.Response.ResponseDevice>>
        {
            Data = devices
        };
    }

    /// <summary>
    /// Get a device by its id
    /// </summary>
    /// <param name="deviceId"></param>
    /// <response code="200">The device</response>
    [HttpGet("{deviceId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<Models.Response.ResponseDeviceWithToken>> GetDeviceById([FromRoute] Guid deviceId)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == deviceId)
            .Select(x => new Models.Response.ResponseDeviceWithToken
            {
                Id = x.Id,
                Name = x.Name,
                CreatedOn = x.CreatedOn,
                Token = x.Token
            }).SingleOrDefaultAsync();
        if (device == null)
            return EBaseResponse<Models.Response.ResponseDeviceWithToken>("Device does not exist",
                HttpStatusCode.NotFound);
        return new BaseResponse<Models.Response.ResponseDeviceWithToken>
        {
            Data = device
        };
    }

    /// <summary>
    /// Edit a device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="body"></param>
    /// <param name="redisPubService"></param>
    /// <response code="200">Successfully updated device</response>
    /// <response code="404">Device does not exist</response>
    [HttpPatch("{deviceId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> EditDevice([FromRoute] Guid deviceId, [FromBody] DeviceEdit body, [FromServices] IRedisPubService redisPubService)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == deviceId)
            .SingleOrDefaultAsync();
        if (device == null)
            return EBaseResponse<object>("Device does not exist", HttpStatusCode.NotFound);

        device.Name = body.Name;
        await _db.SaveChangesAsync();

        await redisPubService.SendDeviceUpdate(device.Id);
        
        return new BaseResponse<object>
        {
            Message = "Successfully updated device"
        };
    }

    /// <summary>
    /// Regenerate a device token
    /// </summary>
    /// <param name="deviceId">The id of the device to regenerate the token for</param>
    /// <response code="200">Successfully regenerated device token</response>
    /// <response code="404">Device does not exist</response>
    /// <response code="500">Failed to save regenerated token</response>
    [HttpPut("{deviceId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<BaseResponse<object>> RegenerateDeviceToken([FromRoute] Guid deviceId)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == deviceId)
            .SingleOrDefaultAsync();
        if (device == null)
            return EBaseResponse<object>("Device does not exist", HttpStatusCode.NotFound);

        device.Token = CryptoUtils.RandomString(256);

        var affected = await _db.SaveChangesAsync();
        if (affected <= 0)
            return EBaseResponse<object>("Failed to save regenerated token", HttpStatusCode.InternalServerError);

        return new BaseResponse<object>
        {
            Message = "Successfully regenerated device token"
        };
    }

    /// <summary>
    /// Remove a device from current user's account
    /// </summary>
    /// <param name="deviceId">The id of the device to delete</param>
    /// <param name="redisPubService"></param>
    /// <response code="200">Successfully deleted device</response>
    /// <response code="404">Device does not exist</response>
    [HttpDelete("{deviceId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> RemoveDevice([FromRoute] Guid deviceId, [FromServices] IRedisPubService redisPubService)
    {
        var affected = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == deviceId)
            .ExecuteDeleteAsync();
        if (affected <= 0)
            return EBaseResponse<object>("Device does not exist", HttpStatusCode.NotFound);
        
        await redisPubService.SendDeviceUpdate(deviceId);
        
        return new BaseResponse<object>
        {
            Message = "Successfully deleted device"
        };
    }

    /// <summary>
    /// Create a new device for the current user
    /// </summary>
    /// <response code="201">Successfully created device</response>
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<BaseResponse<Guid>> CreateDevice([FromServices] IRedisPubService redisPubService)
    {
        var device = new Common.OpenShockDb.Device
        {
            Id = Guid.NewGuid(),
            Owner = CurrentUser.DbUser.Id,
            Name = $"New Device {DateTimeOffset.UtcNow}",
            Token = CryptoUtils.RandomString(256)
        };
        _db.Devices.Add(device);
        await _db.SaveChangesAsync();
        
        await redisPubService.SendDeviceUpdate(device.Id);
        
        Response.StatusCode = (int)HttpStatusCode.Created;
        return new BaseResponse<Guid>
        {
            Message = "Successfully created device",
            Data = device.Id
        };
    }

    /// <summary>
    /// Get a pair code for a device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <response code="200">The pair code</response>
    /// <response code="404">Device does not exist or does not belong to you</response>
    [HttpGet("{deviceId}/pair")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<string>> GetPairCode([FromRoute] Guid deviceId)
    {
        var devicePairs = _redis.RedisCollection<DevicePair>();

        var deviceExists = await _db.Devices.AnyAsync(x => x.Id == deviceId && x.Owner == CurrentUser.DbUser.Id);
        if (!deviceExists)
            return EBaseResponse<string>("Device does not exists or does not belong to you", HttpStatusCode.NotFound);
        // replace with unlink?
        var existing = await devicePairs.FindByIdAsync(deviceId.ToString());
        if (existing != null) await devicePairs.DeleteAsync(existing);

        var r = new Random();
        var pairCode = new DevicePair
        {
            Id = deviceId,
            PairCode = r.Next(0, 1000000).ToString("000000")
        };
        await devicePairs.InsertAsync(pairCode, TimeSpan.FromMinutes(15));

        return new BaseResponse<string>
        {
            Data = pairCode.PairCode
        };
    }

    /// <summary>
    /// Get LCG info for a device if it is online and connected to a LCG node
    /// </summary>
    /// <param name="deviceId"></param>
    /// <response code="200">LCG node was found and device is online</response>
    /// <response code="404">Device does not exist or does not belong to you</response>
    /// <response code="404">Device is not online</response>
    /// <response code="412">Device is online but not connected to a LCG node, you might need to upgrade your firmware to use this feature</response>
    /// <response code="500">Internal server error, lcg node could not be found</response>
    [HttpGet("{deviceId}/lcg")]
    public async Task<BaseResponse<LcgResponse>> GetLiveControlGatewayInfo(Guid deviceId)
    {
        // Check if user owns device or has a share
        var deviceExistsAndYouHaveAccess = await _db.Devices.AnyAsync(x =>
            x.Id == deviceId && (x.Owner == CurrentUser.DbUser.Id || x.Shockers.Any(y => y.ShockerShares.Any(
                z => z.SharedWith == CurrentUser.DbUser.Id))));
        if (!deviceExistsAndYouHaveAccess)
            return EBaseResponse<LcgResponse>("Device does not exists or does not belong to you",
                HttpStatusCode.NotFound);

        // Check if device is online
        var devicesOnline = _redis.RedisCollection<DeviceOnline>();
        var online = await devicesOnline.FindByIdAsync(deviceId.ToString());
        if (online == null) return EBaseResponse<LcgResponse>("Device is not online", HttpStatusCode.NotFound);

        // Check if device is connected to a LCG node
        if (online.Gateway == null)
            return EBaseResponse<LcgResponse>(
                "Device is online but not connected to a LCG node, you might need to upgrade your firmware to use this feature",
                HttpStatusCode.PreconditionFailed);

        // Get LCG node info
        var lcgNodes = _redis.RedisCollection<LcgNode>();
        var gateway = await lcgNodes.FindByIdAsync(online.Gateway);
        if (gateway == null)
            return EBaseResponse<LcgResponse>("Internal server error, lcg node could not be found",
                HttpStatusCode.InternalServerError);


        return new BaseResponse<LcgResponse>
        {
            Data = new LcgResponse
            {
                Gateway = gateway.Fqdn,
                Country = gateway.Country
            }
        };
    }
}