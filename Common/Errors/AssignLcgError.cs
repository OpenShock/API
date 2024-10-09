using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class AssignLcgError
{
    public static OpenShockProblem NoLcgNodesAvailable => new("AssignLcg.NoLcgAvailable", "No LCG node available", HttpStatusCode.ServiceUnavailable);
}