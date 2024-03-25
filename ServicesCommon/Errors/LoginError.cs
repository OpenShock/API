using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class LoginError
{
    public static OpenShockProblem InvalidCredentials => new OpenShockProblem("Login.InvalidCredentials", "Invalid credentials provided", HttpStatusCode.Unauthorized);
}