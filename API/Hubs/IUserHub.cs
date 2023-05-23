using ShockLink.API.Models.WebSocket;

namespace ShockLink.API.Hubs;

public interface IUserHub
{
    Task DeviceStatus(IEnumerable<DeviceOnlineState> deviceOnlineStates);

    Task Log(IEnumerable<Common.Models.WebSocket.User.Control> control);
}