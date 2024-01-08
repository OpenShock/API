using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.User;
using Semver;

namespace OpenShock.ServicesCommon.Hubs;

public interface IUserHub
{
    Task Welcome(string connectionId);
    Task DeviceStatus(IEnumerable<DeviceOnlineState> deviceOnlineStates);
    Task Log(ControlLogSender sender, IEnumerable<ControlLog> logs);
    Task DeviceUpdate(Guid deviceId, DeviceUpdateType type);
    
    // OTA
    Task OtaInstallStarted(Guid deviceId, SemVersion version);
    Task OtaInstallProgress(Guid deviceId, string task, float progress);
    Task OtaInstallFailed(Guid deviceId, bool bricked, string message);
    Task OtaInstallSucceeded(Guid deviceId, SemVersion version);
}