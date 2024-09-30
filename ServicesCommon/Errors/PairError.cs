using OpenShock.ServicesCommon.Problems;
using System.Net;

namespace OpenShock.ServicesCommon.Errors;

public static class PairError
{
    public static OpenShockProblem PairCodeNotFound => new("Pair.CodeNotFound", "Pair code not found", HttpStatusCode.NotFound);
}