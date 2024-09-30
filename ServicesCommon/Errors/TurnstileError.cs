using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class TurnstileError
{
    public static OpenShockProblem InvalidTurnstile => new("Turnstile.Invalid", "Invalid turnstile response", HttpStatusCode.Forbidden);
}