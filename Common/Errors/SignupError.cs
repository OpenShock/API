using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class SignupError
{
    public static OpenShockProblem EmailAlreadyExists => new("Signup.EmailOrUsernameAlreadyExists", "Email or username already exists", HttpStatusCode.Conflict);
}