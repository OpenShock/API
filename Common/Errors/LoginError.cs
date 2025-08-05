using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class LoginError
{
    public static OpenShockProblem AccountNotActivated => new OpenShockProblem("Login.AccountNotActivated", "Account has not been activated", HttpStatusCode.Unauthorized);
    public static OpenShockProblem AccountDeactivated => new OpenShockProblem("Login.AccountDeactivated", "Account has been deactivated", HttpStatusCode.Unauthorized);
    public static OpenShockProblem InvalidCredentials => new OpenShockProblem("Login.InvalidCredentials", "Invalid credentials provided", HttpStatusCode.Unauthorized);
    public static OpenShockProblem InvalidDomain => new OpenShockProblem("Login.InvalidDomain", "The url you are requesting a login from is not whitelisted", HttpStatusCode.Forbidden);
}