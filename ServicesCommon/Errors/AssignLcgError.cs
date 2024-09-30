using OpenShock.ServicesCommon.Problems;
using System.Net;

namespace OpenShock.ServicesCommon.Errors;

public static class AssignLcgError
{
    public static OpenShockProblem NoLcgNodesAvailable => new("AssignLcg.NoLcgAvailable", "No LCG node available", HttpStatusCode.ServiceUnavailable);
}