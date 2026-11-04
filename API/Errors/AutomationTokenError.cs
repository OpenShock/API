using System.Net;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Errors;

public static class AutomationTokenError
{
    public static OpenShockProblem NotAllowedForAccount => new("AutomationToken.NotAllowedForAccount", "Automation tokens cannot be used with privileged accounts", HttpStatusCode.Forbidden);
}
