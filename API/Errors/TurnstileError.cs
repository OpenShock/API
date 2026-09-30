using System.Net;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Errors;

public static class TurnstileError
{
    public static OpenShockProblem InvalidTurnstile => new("Turnstile.Invalid", "Invalid turnstile response", HttpStatusCode.Forbidden);
}