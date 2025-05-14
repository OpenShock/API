using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class DeleteAccountError
{
    public static OpenShockProblem CannotDeactivatePrivledgedAccount => new OpenShockProblem(
        "Account.Deactivate.DeniedPrivileged", "Privileged accounts cannot be deactivated!", HttpStatusCode.Forbidden);
}