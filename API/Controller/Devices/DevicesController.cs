using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Realtime;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Redis.PubSub;
using OpenShock.ServicesCommon.Authentication;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.Controller.Devices;

public sealed partial class DevicesController
{
    /// <summary>
    /// Gets all devices for the current user
    /// </summary>
    /// <response code="200">All devices for the current user</response>
    [HttpGet(Name = "GetDevices")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<IEnumerable<Models.Response.ResponseDevice>>> GetList()
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
    /// Gets a device by id
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">The device</response>
    [HttpGet("{id}", Name = "GetDevice")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<Models.Response.ResponseDeviceWithToken>> Get([FromRoute] Guid id)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == id)
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
    /// Edits a device
    /// </summary>
    /// <response code="200">Successfully updated device</response>
    /// <response code="404">Device does not exist</response>
    [HttpPatch("{id}", Name = "EditDevice")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> Edit([FromRoute] Guid id, [FromBody] DeviceEdit data)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == id)
            .SingleOrDefaultAsync();
        if (device == null)
            return EBaseResponse<object>("Device does not exist", HttpStatusCode.NotFound);

        device.Name = data.Name;
        await _db.SaveChangesAsync();

        await PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
        {
            Id = device.Id
        });
        
        return new BaseResponse<object>
        {
            Message = "Successfully updated device"
        };
    }

    /// <summary>
    /// Regenerates a device token
    /// </summary>
    /// <param name="id">The id of the device to regenerate the token for</param>
    /// <response code="200">Successfully regenerated device token</response>
    /// <response code="404">Device does not exist</response>
    /// <response code="500">Failed to save regenerated token</response>
    [HttpPut("{id}", Name = "RegenDeviceToken")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<BaseResponse<object>> RegenToken([FromRoute] Guid id)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == id)
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
    /// Deletes a device
    /// </summary>
    /// <param name="id">The id of the device to delete</param>
    /// <response code="200">Successfully deleted device</response>
    /// <response code="404">Device does not exist</response>
    [HttpDelete("{id}", Name = "DeleteDevice")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> Delete([FromRoute] Guid id)
    {
        var affected = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == id)
            .ExecuteDeleteAsync();
        if (affected <= 0)
            return EBaseResponse<object>("Device does not exist", HttpStatusCode.NotFound);
        
        await PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
        {
            Id = id
        });
        
        return new BaseResponse<object>
        {
            Message = "Successfully deleted device"
        };
    }

    /// <summary>
    /// Creates a new device
    /// </summary>
    /// <response code="201">Successfully created device</response>
    [HttpPost(Name = "CreateDevice")]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<BaseResponse<Guid>> CreateDevice()
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

        await PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
        {
            Id = device.Id
        });
        
        Response.StatusCode = (int)HttpStatusCode.Created;
        return new BaseResponse<Guid>
        {
            Message = "Successfully created device",
            Data = device.Id
        };
    }

    /// <summary>
    /// Gets a pair code for a device
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">The pair code</response>
    /// <response code="404">Device does not exist or does not belong to you</response>
    [HttpGet("{id}/pair", Name = "GetPairCode")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<string>> GetPairCode([FromRoute] Guid id)
    {
        var devicePairs = _redis.RedisCollection<DevicePair>();

        var deviceExists = await _db.Devices.AnyAsync(x => x.Id == id && x.Owner == CurrentUser.DbUser.Id);
        if (!deviceExists)
            return EBaseResponse<string>("Device does not exists or does not belong to you", HttpStatusCode.NotFound);
        // replace with unlink?
        var existing = await devicePairs.FindByIdAsync(id.ToString());
        if (existing != null) await devicePairs.DeleteAsync(existing);

        var r = new Random();
        var pairCode = new DevicePair
        {
            Id = id,
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
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/lcg")]
    public async Task<BaseResponse<LcgResponse>> GetLcgInfo(Guid id)
    {
        // Check if user owns device or has a share
        var deviceExistsAndYouHaveAccess = await _db.Devices.AnyAsync(x =>
            x.Id == id && (x.Owner == CurrentUser.DbUser.Id || x.Shockers.Any(y => y.ShockerShares.Any(
                z => z.SharedWith == CurrentUser.DbUser.Id))));
        if (!deviceExistsAndYouHaveAccess)
            return EBaseResponse<LcgResponse>("Device does not exists or does not belong to you",
                HttpStatusCode.NotFound);

        // Check if device is online
        var devicesOnline = _redis.RedisCollection<DeviceOnline>();
        var online = await devicesOnline.FindByIdAsync(id.ToString());
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