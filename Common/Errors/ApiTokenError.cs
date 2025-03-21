using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class ApiTokenError
{
    public static OpenShockProblem ApiTokenNotFound => new("ApiToken.NotFound", "Api token not found", HttpStatusCode.NotFound);
    public static OpenShockProblem ApiTokenCanOnlyDelete => new("ApiToken.CanOnlyDelete own", "You can only delete your own api token in token authentication scope", HttpStatusCode.Forbidden);
}