using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class ShockerError
{
    public static OpenShockProblem ShockerNotFound => new("Shocker.NotFound", "Shocker not found", HttpStatusCode.NotFound);
}