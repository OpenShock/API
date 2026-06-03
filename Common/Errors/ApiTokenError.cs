using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class ApiTokenError
{
    public static OpenShockProblem ApiTokenNotFound => new("ApiToken.NotFound", "Api token not found", HttpStatusCode.NotFound);
    public static OpenShockProblem ApiTokenCanOnlyDeleteSelf => new("ApiToken.CanOnlyDeleteSelf", "You can only delete your own api token in token authentication scope", HttpStatusCode.Forbidden);
    public static OpenShockProblem ApiTokenPaused => new("ApiToken.Paused", "This api token is paused and may not control shockers", HttpStatusCode.Forbidden);
}