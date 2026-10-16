namespace OpenShock.Common.Services.Device;

public interface IDeviceService
{
    /// <summary>
    /// Get all users that have a share (for a shocker) within the device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IList<Guid>> GetSharedUserIdsAsync(Guid deviceId, CancellationToken cancellationToken = default);
}