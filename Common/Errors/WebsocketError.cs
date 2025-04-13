using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class WebsocketError
{
    public static OpenShockProblem NonWebsocketRequest => new("Websocket.NonWebsocketRequest", "This request is not a websocket request", HttpStatusCode.BadRequest);
    
    public static OpenShockProblem WebsocketHubFirmwareVersionInvalid => new("Websocket.Hub.FirmwareVersionInvalid", "Supplied firmware version header is not valid semver", HttpStatusCode.BadRequest);
    public static OpenShockProblem WebsocketLiveControlHubIdInvalid => new("Websocket.LiveControl.HubIdInvalid", "Hub (device) id was missing or invalid", HttpStatusCode.BadRequest);
    public static OpenShockProblem WebsocketLiveControlHubNotFound => new("Websocket.LiveControl.HubNotFound", "Hub was not found or you are missing access", HttpStatusCode.NotFound);
    public static OpenShockProblem WebsocketLiveControlHubNotConnected => new("Websocket.LiveControl.HubNotConnected", "The requested Hub is not connected to this gateway", HttpStatusCode.NotFound);
    public static OpenShockProblem WebsocketLiveControlHubLifetimeBusy => new("Websocket.LiveControl.HubLifetimeBusy", "Hub Lifetime is currently busy, try again please", HttpStatusCode.PreconditionFailed);
    public static OpenShockProblem WebsocketHubLifetimeBusy => new("Websocket.Hub.LifetimeBusy", "Hub Lifetime is currently busy, try again soon please", HttpStatusCode.PreconditionFailed);
}