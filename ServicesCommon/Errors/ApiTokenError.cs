using OpenShock.ServicesCommon.Problems;
using System.Net;

namespace OpenShock.ServicesCommon.Errors;

public static class ApiTokenError
{
    public static OpenShockProblem ApiTokenNotFound => new("ApiToken.NotFound", "Api token not found", HttpStatusCode.NotFound);
}