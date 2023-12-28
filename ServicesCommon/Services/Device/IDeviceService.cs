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

    /// <summary>
    /// When anything about the device or its shockers is updated
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="deviceId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Task UpdateDevice(Guid ownerId, Guid deviceId, DeviceUpdateType type);
}