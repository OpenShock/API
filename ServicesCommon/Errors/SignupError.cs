using OpenShock.ServicesCommon.Problems;
using System.Net;

namespace OpenShock.ServicesCommon.Errors;

public static class SignupError
{
    public static OpenShockProblem EmailAlreadyExists => new("Signup.EmailOrUsernameAlreadyExists", "Email or username already exists", HttpStatusCode.Conflict);
}