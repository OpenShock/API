using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class WebsocketError
{
    public static OpenShockProblem NonWebsocketRequest => new("Websocket.NonWebsocketRequest", "This request is not a websocket request", HttpStatusCode.BadRequest);
}