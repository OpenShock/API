using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Services.RedisPubSub;

namespace OpenShock.ServicesCommon.Services.Device;

public class DeviceService : IDeviceService
{
    private readonly OpenShockContext _db;
    private readonly IRedisPubService _redisPubService;
    private readonly IHubContext<UserHub, IUserHub> _hubContext;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="redisPubService"></param>
    /// <param name="hubContext"></param>
    public DeviceService(OpenShockContext db, IRedisPubService redisPubService, IHubContext<UserHub, IUserHub> hubContext)
    {
        _db = db;
        _redisPubService = redisPubService;
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task<IList<Guid>> GetSharedUsers(Guid deviceId)
    {
        var sharedUsers = await _db.ShockerShares.AsNoTracking().Where(x => x.Shocker.Device == deviceId).GroupBy(x => x.SharedWith)
            .Select(x => x.Key)
            .ToListAsync();
        return sharedUsers;
    }

    /// <inheritdoc />
    public async Task UpdateDevice(Guid ownerId, Guid deviceId, DeviceUpdateType type, Guid affectedUser)
    {
        var task1 = _redisPubService.SendDeviceUpdate(deviceId);
        var task2 = _hubContext.Clients.Users(ownerId.ToString(), affectedUser.ToString())
            .DeviceUpdate(deviceId, DeviceUpdateType.ShockerUpdated);
        await Task.WhenAll(task1, task2);
    }

    /// <inheritdoc />
    public async Task UpdateDeviceForAllShared(Guid ownerId, Guid deviceId, DeviceUpdateType type)
    {
        var task1 = _redisPubService.SendDeviceUpdate(deviceId);
        
        var sharedWith = await GetSharedUsers(deviceId);
        sharedWith.Add(ownerId); // Add the owner to the list of users to send to
        var task2 = _hubContext.Clients.Users(sharedWith.Select(x => x.ToString()))
            .DeviceUpdate(deviceId, DeviceUpdateType.ShockerUpdated);
        
        await Task.WhenAll(task1, task2);
    }
}