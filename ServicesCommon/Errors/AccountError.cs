using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class AccountError
{
    public static OpenShockProblem UsernameTaken => new OpenShockProblem("Account.Username.Taken",
        "This username is already in use", HttpStatusCode.Conflict);

    public static OpenShockProblem UsernameUnavailable => new OpenShockProblem("Account.Username.Unavailable",
        "This username is unavailable", HttpStatusCode.Conflict);
}