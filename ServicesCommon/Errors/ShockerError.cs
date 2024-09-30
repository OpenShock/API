using OpenShock.ServicesCommon.Problems;
using System.Net;

namespace OpenShock.ServicesCommon.Errors;

public static class ShockerError
{
    public static OpenShockProblem ShockerNotFound => new("Shocker.NotFound", "Shocker not found", HttpStatusCode.NotFound);
}