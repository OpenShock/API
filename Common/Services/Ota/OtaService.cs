using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.Models.Services.Ota;
using OpenShock.Common.OpenShockDb;
using Semver;

namespace OpenShock.Common.Services.Ota;

public sealed class OtaService : IOtaService
{
    private readonly OpenShockContext _db;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"></param>
    public OtaService(OpenShockContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task Started(Guid deviceId, int updateId, SemVersion version)
    {
        _db.DeviceOtaUpdates.Add(new DeviceOtaUpdate
        {
            Device = deviceId,
            UpdateId = updateId,
            Status = OtaUpdateStatus.Started,
            Version = version.ToString()
        });

        return _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task Progress(Guid deviceId, int updateId)
    {
        var updateTask = await _db.DeviceOtaUpdates
            .Where(x => x.Device == deviceId && x.UpdateId == updateId)
            .FirstOrDefaultAsync();
        if (updateTask == null) return;
        updateTask.Status = OtaUpdateStatus.Running;

        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task Error(Guid deviceId, int updateId, bool fatal, string message)
    {
        var updateTask = await _db.DeviceOtaUpdates
            .Where(x => x.Device == deviceId && x.UpdateId == updateId)
            .FirstOrDefaultAsync();
        if (updateTask == null) return;
        updateTask.Status = OtaUpdateStatus.Error;
        updateTask.Message = message;

        await _db.SaveChangesAsync();
    }
    
    /// <inheritdoc />
    public async Task<bool> Success(Guid deviceId, int updateId)
    {
        var updateTask = await _db.DeviceOtaUpdates
            .Where(x => x.Device == deviceId && x.UpdateId == updateId)
            .FirstOrDefaultAsync();
        if (updateTask == null) return false;
        updateTask.Status = OtaUpdateStatus.Finished;

        await _db.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<OtaItem>> GetUpdates(Guid deviceId)
    {
        return await _db.DeviceOtaUpdates.AsNoTracking()
            .Where(x => x.Device == deviceId)
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new OtaItem
        {
            Id = x.UpdateId,
            StartedAt = x.CreatedOn,
            Status = x.Status,
            Version = SemVersion.Parse(x.Version, SemVersionStyles.Strict, 1024),
            Message = x.Message
        }).ToArrayAsync();
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUnfinished(Guid deviceId, int updateId)
    {
        return await _db.DeviceOtaUpdates.AsNoTracking()
            .AnyAsync(x => x.Device == deviceId &&
                           x.UpdateId == updateId &&
                           (x.Status == OtaUpdateStatus.Running || x.Status == OtaUpdateStatus.Started));
    }
}