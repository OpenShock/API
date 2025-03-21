using OpenShock.Common.Redis.PubSub;
using Semver;

namespace OpenShock.Common.Services.RedisPubSub;

public interface IRedisPubService
{
    /// <summary>
    /// Used when a device comes online or changes its connection details like, gateway, firmware version, etc.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public Task SendDeviceOnlineStatus(Guid deviceId);

    /// <summary>
    /// General shocker control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="controlMessages"></param>
    /// <returns></returns>
    Task SendDeviceControl(Guid sender,
        IDictionary<Guid, IReadOnlyList<ControlMessage.ShockerControlInfo>> controlMessages);
    
    /// <summary>
    /// Toggle captive portal
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    Task SendDeviceCaptivePortal(Guid deviceId, bool enabled);
    
    /// <summary>
    /// Something about the device or its shockers updated
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    Task SendDeviceUpdate(Guid deviceId);

    /// <summary>
    /// Start an OTA update on the device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    Task SendDeviceOtaInstall(Guid deviceId, SemVersion version);
}