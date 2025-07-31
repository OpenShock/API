using System.Net;
using OpenShock.Common.Constants;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class HubError
{
    public static OpenShockProblem HubNotFound => new("Device.NotFound", "Hub not found", HttpStatusCode.NotFound);
    public static OpenShockProblem HubIsNotOnline => new("Device.NotOnline", "Hub is not online", HttpStatusCode.NotFound);
    public static OpenShockProblem TooManyHubs => new("Device.TooManyHubs", "You have too many hubs", HttpStatusCode.Conflict, $"You have reached the maximum number of shockers for this hub ({HardLimits.MaxHubsPerUser})");
    public static OpenShockProblem TooManyShockers => new("Device.TooManyShockers", "Hub has too many shockers", HttpStatusCode.BadRequest, $"You have reached the maximum number of shockers for this hub ({HardLimits.MaxShockersPerHub})");
    
}