using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class WebsocketError
{
    public static OpenShockProblem NonWebsocketRequest => new("Websocket.NonWebsocketRequest", "This request is not a websocket request", HttpStatusCode.BadRequest);
}