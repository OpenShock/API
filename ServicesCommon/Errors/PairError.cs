using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class PairError
{
    public static OpenShockProblem PairCodeNotFound => new("Pair.CodeNotFound", "Pair code not found", HttpStatusCode.NotFound);
}