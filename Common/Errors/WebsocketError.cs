using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class WebsocketError
{
    public static OpenShockProblem NonWebsocketRequest => new("Websocket.NonWebsocketRequest", "This request is not a websocket request", HttpStatusCode.BadRequest);
    
    public static OpenShockProblem WebsocketHubFirmwareVersionInvalid => new("Websocket.Hub.FirmwareVersionInvalid", "Supplied firmware version header is not valid semver", HttpStatusCode.BadRequest);
    public static OpenShockProblem WebsocketLiveControlHubIdInvalid => new("Websocket.LiveControl.HubIdInvalid", "Hub (device) id was missing or invalid", HttpStatusCode.BadRequest);
    public static OpenShockProblem WebsocketLiveControlHubNotFound => new("Websocket.LiveControl.HubNotFound", "Hub was not found or you are missing access", HttpStatusCode.NotFound);
}