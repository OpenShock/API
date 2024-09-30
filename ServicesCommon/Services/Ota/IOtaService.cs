using OpenShock.Common.Models.Services.Ota;
using Semver;

namespace OpenShock.ServicesCommon.Services.Ota;

public interface IOtaService
{
    /// <summary>
    /// When the OTA updated started, triggered by the device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public Task Started(Guid deviceId, int updateId, SemVersion version);

    /// <summary>
    /// When the first progress is reported, set the state to running
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <returns></returns>
    public Task Progress(Guid deviceId, int updateId);

    /// <summary>
    /// When an error or rollback occured during the update
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <returns></returns>
    public Task Error(Guid deviceId, int updateId, bool fatal, string message);

    /// <summary>
    /// Updated succeeded
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <returns></returns>
    public Task<bool> Success(Guid deviceId, int updateId);

    /// <summary>
    /// Check for an unfinished update
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <returns></returns>
    public Task<bool> UpdateUnfinished(Guid deviceId, int updateId);

    /// <summary>
    /// Get all updates for a device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public Task<IReadOnlyCollection<OtaItem>> GetUpdates(Guid deviceId);
}