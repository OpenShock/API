using System.Net;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class SignupError
{
    public static OpenShockProblem EmailAlreadyExists => new("Signup.EmailOrUsernameAlreadyExists", "Email or username already exists", HttpStatusCode.Conflict);
}