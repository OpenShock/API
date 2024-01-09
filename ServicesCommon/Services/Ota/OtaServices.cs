using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using Semver;

namespace OpenShock.ServicesCommon.Services.Ota;

public class OtaServices : IOtaService
{
    private readonly OpenShockContext _db;

    public OtaServices(OpenShockContext db)
    {
        _db = db;
    }

    public async Task Request(Guid deviceId, SemVersion version)
    {
        // Set all previous ones to timeout that are still hot
        await _db.DeviceOtaUpdates.Where(x =>
                x.Device == deviceId &&
                x.Status == OtaUpdateStatus.Requested &&
                x.Status == OtaUpdateStatus.Started &&
                x.Status == OtaUpdateStatus.Running)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.Status, OtaUpdateStatus.Timeout));

        _db.DeviceOtaUpdates.Add(new DeviceOtaUpdate
        {
            Device = deviceId,
            UpdateId = Guid.NewGuid(),
            Status = OtaUpdateStatus.Requested,
            Version = version.ToString()
        });

        await _db.SaveChangesAsync();
    }

    public Task Started(Guid deviceId, SemVersion version)
    {
        _db.DeviceOtaUpdates.Where(x =>
            x.Device == deviceId &&
            x.Status == OtaUpdateStatus.Requested
        ).OrderByDescending(x => x.CreatedOn).FirstOrDefaultAsync();
        
        
        
        
        return null;
    }
}