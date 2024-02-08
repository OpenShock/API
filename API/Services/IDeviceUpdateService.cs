using OpenShock.Common.Models;

namespace OpenShock.API.Services;

public interface IDeviceUpdateService
{
    /// <summary>
    /// When anything about the device or its shockers is updated, sent to owner and affected user
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="deviceId"></param>
    /// <param name="type"></param>
    /// <param name="affectedUser"></param>
    /// <returns></returns>
    public Task UpdateDevice(Guid ownerId, Guid deviceId, DeviceUpdateType type, Guid affectedUser);
    
    /// <summary>
    /// When anything about the device or its shockers is updated, sent to owner, usually only used on device creation
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="deviceId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Task UpdateDevice(Guid ownerId, Guid deviceId, DeviceUpdateType type);
    
    /// <summary>
    /// When anything about the device or its shockers is updated, sent to all shared users and owner
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="deviceId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Task UpdateDeviceForAllShared(Guid ownerId, Guid deviceId, DeviceUpdateType type);
}