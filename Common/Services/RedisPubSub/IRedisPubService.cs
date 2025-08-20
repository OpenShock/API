using OpenShock.Common.Redis.PubSub;
using Semver;

namespace OpenShock.Common.Services.RedisPubSub;

public interface IRedisPubService
{
    /// <summary>
    /// Something about the device or its shockers updated
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    Task SendDeviceUpdate(Guid deviceId);

    /// <summary>
    /// Used when a device comes online or changes its connection details like, gateway, firmware version, etc.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="isOnline"></param>
    /// <returns></returns>
    public Task SendDeviceOnlineStatus(Guid deviceId, bool isOnline);

    /// <summary>
    /// General shocker control
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="controls"></param>
    /// <returns></returns>
    Task SendDeviceControl(Guid deviceId, List<DeviceControlPayload.ShockerControlInfo> controls);

    /// <summary>
    /// Toggle captive portal
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    Task SendDeviceCaptivePortal(Guid deviceId, bool enabled);

    /// <summary>
    /// Trigger the emergency stop on the device if it's supported
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    Task SendDeviceEmergencyStop(Guid deviceId);

    /// <summary>
    /// Start an OTA update on the device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    Task SendDeviceOtaInstall(Guid deviceId, SemVersion version);

    /// <summary>
    /// Reboot the device if it's supported
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    Task SendDeviceReboot(Guid deviceId);
}