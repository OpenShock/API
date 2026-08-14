using OpenShock.Common.Models;
using OpenShock.Common.Redis.PubSub;

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
    /// The shocker-control scoping (limits, pause state) of an API token changed, or the token was revoked.
    /// Notifies live control gateways so open connections using the token refetch their limits.
    /// </summary>
    /// <param name="tokenId"></param>
    /// <returns></returns>
    Task SendApiTokenUpdate(Guid tokenId);

    /// <summary>
    /// Notifies the email outbox consumer that one or more messages were just enqueued, so it drains
    /// them without waiting for its next safety-net poll. Best-effort: a lost notification only delays
    /// delivery until the next poll, it cannot lose the email (the row is already durably committed).
    /// </summary>
    /// <returns></returns>
    Task SendEmailOutboxPending();

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
    Task SendDeviceControl(Guid deviceId, List<ShockerControlCommand> controls);

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