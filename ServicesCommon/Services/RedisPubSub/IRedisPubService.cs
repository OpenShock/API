using OpenShock.Common.Redis.PubSub;

namespace OpenShock.ServicesCommon.Services.RedisPubSub;

public interface IRedisPubService
{
    /// <summary>
    /// General shocker control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="controlMessages"></param>
    /// <returns></returns>
    Task SendDeviceControl(Guid sender, IDictionary<Guid, IList<ControlMessage.ShockerControlInfo>> controlMessages);
    
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
}