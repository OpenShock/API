using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class DeviceError
{
    public static OpenShockProblem DeviceNotFound => new("Device.NotFound", "Device not found", HttpStatusCode.NotFound);
    public static OpenShockProblem DeviceIsNotOnline => new("Device.NotOnline", "Device is not online", HttpStatusCode.NotFound);
    public static OpenShockProblem DeviceNotConnectedToGateway => new("Device.NotConnectedToGateway", "Device is not connected to a gateway", HttpStatusCode.PreconditionFailed, "Device is online but not connected to a LCG node, you might need to upgrade your firmware to use this feature");
    
    public static OpenShockProblem TooManyShockers => new("Device.TooManyShockers", "Device has too many shockers", HttpStatusCode.BadRequest, "You have reached the maximum number of shockers for this device (11)");
    
}