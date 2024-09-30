using OpenShock.ServicesCommon.Problems;
using System.Net;

namespace OpenShock.ServicesCommon.Errors;

public static class LoginError
{
    public static OpenShockProblem InvalidCredentials => new OpenShockProblem("Login.InvalidCredentials", "Invalid credentials provided", HttpStatusCode.Unauthorized);
    public static OpenShockProblem InvalidDomain => new OpenShockProblem("Login.InvalidDomain", "The url you are requesting a login from is not whitelisted", HttpStatusCode.Forbidden);
}