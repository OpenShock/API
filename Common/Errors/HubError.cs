using System.Net;
using OpenShock.Common.Constants;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class HubError
{
    public static OpenShockProblem HubNotFound => new("Hub.NotFound", "Hub not found", HttpStatusCode.NotFound);
    public static OpenShockProblem HubIsNotOnline => new("Hub.NotOnline", "Hub is not online", HttpStatusCode.NotFound);
    public static OpenShockProblem HubNotConnectedToGateway => new("Hub.NotConnectedToGateway", "Hub is not connected to a gateway", HttpStatusCode.PreconditionFailed, "Hub is online but not connected to a LCG node, you might need to upgrade your firmware to use this feature");

    public static OpenShockProblem TooManyHubs => new("Hub.TooManyHubs", "You have too many hubs", HttpStatusCode.Conflict, $"You have reached the maximum number of shockers for this hub ({HardLimits.MaxHubsPerUser})");
    public static OpenShockProblem TooManyShockers => new("Hub.TooManyShockers", "Hub has too many shockers", HttpStatusCode.BadRequest, $"You have reached the maximum number of shockers for this hub ({HardLimits.MaxShockersPerHub})");
    
}