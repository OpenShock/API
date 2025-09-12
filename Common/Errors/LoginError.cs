using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class LoginError
{
    public static OpenShockProblem InvalidCredentials => new OpenShockProblem("Login.InvalidCredentials", "Invalid username or password", HttpStatusCode.Unauthorized);
    public static OpenShockProblem InvalidDomain => new OpenShockProblem("Login.InvalidDomain", "The url you are requesting a login from is not whitelisted", HttpStatusCode.Forbidden);
}