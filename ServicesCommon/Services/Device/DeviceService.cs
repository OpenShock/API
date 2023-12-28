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
    public DeviceService(OpenShockContext db, IRedisPubService redisPubService)
    {
        _db = db;
        _redisPubService = redisPubService;
    }

    /// <inheritdoc />
    public async Task<IList<Guid>> GetSharedUsers(Guid deviceId)
    {
        var sharedUsers = await _db.ShockerShares.AsNoTracking().Where(x => x.Shocker.Device == deviceId).GroupBy(x => x.SharedWith)
            .Select(x => x.Key)
            .ToListAsync();
        return sharedUsers;
    }
}