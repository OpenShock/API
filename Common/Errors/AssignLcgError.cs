using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class AssignLcgError
{
    public static OpenShockProblem NoLcgNodesAvailable => new("AssignLcg.NoLcgAvailable", "No LCG node available", HttpStatusCode.ServiceUnavailable);
}