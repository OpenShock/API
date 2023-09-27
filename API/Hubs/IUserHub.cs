using OpenShock.API.Models.WebSocket;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket.User;

namespace OpenShock.API.Hubs;

public interface IUserHub
{
    Task Welcome(string connectionId);
    Task DeviceStatus(IEnumerable<DeviceOnlineState> deviceOnlineStates);
    Task Log(ControlLogSender sender, IEnumerable<ControlLog> logs);
}