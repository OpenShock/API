using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class ShockerError
{
    public static OpenShockProblem ShockerNotFound => new("Shocker.NotFound", "Shocker not found", HttpStatusCode.NotFound);
}