using ShockLink.API.Models.WebSocket;
using ShockLink.Common.Models;
using ShockLink.Common.Models.WebSocket.User;

namespace ShockLink.API.Hubs;

public interface IUserHub
{
    Task Welcome(string connectionId);
    Task DeviceStatus(IEnumerable<DeviceOnlineState> deviceOnlineStates);
    Task Log(ControlLogSender sender, IEnumerable<ControlLog> logs);
}