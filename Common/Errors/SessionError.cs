using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class SessionError
{
    public static OpenShockProblem SessionNotFound => new OpenShockProblem("Session.NotFound",
        "The session was not found", System.Net.HttpStatusCode.NotFound);
}