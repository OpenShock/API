using OpenShock.Common.Models;
using OpenShock.Common.Models.Services.Ota;

namespace OpenShock.Common.Services.Ota;

public interface IOtaService
{
    /// <summary>
    /// When the OTA updated started, triggered by the device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task Started(Guid deviceId, int updateId, SemVersion version, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// When the first progress is reported, set the state to running
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task Progress(Guid deviceId, int updateId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// When an error or rollback occured during the update
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <param name="fatal"></param>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task Error(Guid deviceId, int updateId, bool fatal, string message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updated succeeded
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> Success(Guid deviceId, int updateId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check for an unfinished update
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="updateId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> UpdateUnfinished(Guid deviceId, int updateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all updates for a device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IReadOnlyCollection<OtaItem>> GetUpdates(Guid deviceId, CancellationToken cancellationToken = default);
}