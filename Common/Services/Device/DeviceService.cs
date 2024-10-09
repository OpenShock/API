using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Services.Device;

public sealed class DeviceService : IDeviceService
{
    private readonly OpenShockContext _db;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="db"></param>
    public DeviceService(OpenShockContext db)
    {
        _db = db;
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