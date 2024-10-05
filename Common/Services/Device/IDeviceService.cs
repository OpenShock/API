using OpenShock.Common.Models;

namespace OpenShock.ServicesCommon.Services.Device;

public interface IDeviceService
{
    /// <summary>
    /// Get all users that have a share (for a shocker) within the device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public Task<IList<Guid>> GetSharedUsers(Guid deviceId);
}