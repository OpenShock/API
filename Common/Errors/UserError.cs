using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class UserError
{
    public static OpenShockProblem UserNotFound => new("User.NotFound", "User not found", HttpStatusCode.NotFound);
}