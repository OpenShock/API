using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class PairError
{
    public static OpenShockProblem PairCodeNotFound => new("Pair.CodeNotFound", "Pair code not found", HttpStatusCode.NotFound);
}