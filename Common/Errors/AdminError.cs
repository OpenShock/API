using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class AdminError
{
    public static OpenShockProblem CannotDeletePrivledgedAccount => new OpenShockProblem("User.Privileged.DeleteDenied",
        "You cannot delete a privileged user", HttpStatusCode.Forbidden);
    
    public static OpenShockProblem UserNotFound => new("User.NotFound", "User not found", HttpStatusCode.NotFound);
}