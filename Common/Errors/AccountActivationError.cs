using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class AccountActivationError
{
    public static OpenShockProblem CannotDeactivateOrDeletePrivledgedAccount => new OpenShockProblem(
        "Account.Deactivate.DeniedPrivileged", "Privileged accounts cannot be deactivated/deleted", HttpStatusCode.Forbidden);
    public static OpenShockProblem AlreadyDeactivated => new OpenShockProblem(
        "Account.Deactivate.AlreadyDeactivated", "Account is already deactivated", HttpStatusCode.Forbidden);
    public static OpenShockProblem Unauthorized => new OpenShockProblem(
        "Account.Deactivate.Unauthorized", "You are not allowed to do this", HttpStatusCode.Unauthorized);
}