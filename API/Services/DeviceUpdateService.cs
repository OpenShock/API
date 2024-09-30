using Microsoft.AspNetCore.SignalR;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Services.Device;
using OpenShock.ServicesCommon.Services.RedisPubSub;

namespace OpenShock.API.Services;

public sealed class DeviceUpdateService : IDeviceUpdateService
{
    private readonly IRedisPubService _redisPubService;
    private readonly IHubContext<UserHub, IUserHub> _hubContext;
    private readonly IDeviceService _deviceService;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="redisPubService"></param>
    /// <param name="hubContext"></param>
    /// <param name="deviceService"></param>
    public DeviceUpdateService(
        IRedisPubService redisPubService,
        IHubContext<UserHub, IUserHub> hubContext,
        IDeviceService deviceService)
    {
        _redisPubService = redisPubService;
        _hubContext = hubContext;
        _deviceService = deviceService;
    }
    
    /// <inheritdoc />
    public async Task UpdateDevice(Guid ownerId, Guid deviceId, DeviceUpdateType type, Guid affectedUser)
    {
        var task1 = _redisPubService.SendDeviceUpdate(deviceId);
        var task2 = _hubContext.Clients.Users(ownerId.ToString(), affectedUser.ToString())
            .DeviceUpdate(deviceId, type);
        await Task.WhenAll(task1, task2);
    }

    /// <inheritdoc />
    public async Task UpdateDevice(Guid ownerId, Guid deviceId, DeviceUpdateType type)
    {
        var task1 = _redisPubService.SendDeviceUpdate(deviceId);
        var task2 = _hubContext.Clients.Users(ownerId.ToString())
            .DeviceUpdate(deviceId, type);
        await Task.WhenAll(task1, task2);
    }

    /// <inheritdoc />
    public async Task UpdateDeviceForAllShared(Guid ownerId, Guid deviceId, DeviceUpdateType type)
    {
        var task1 = _redisPubService.SendDeviceUpdate(deviceId);
        
        var sharedWith = await _deviceService.GetSharedUsers(deviceId);
        sharedWith.Add(ownerId); // Add the owner to the list of users to send to
        var task2 = _hubContext.Clients.Users(sharedWith.Select(x => x.ToString()))
            .DeviceUpdate(deviceId, type);
        
        await Task.WhenAll(task1, task2);
    }
}